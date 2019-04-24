namespace Sitecore.Support.XA.Foundation.LocalDatasources.Dialogs
{
  using Sitecore.Data.Items;
  using Sitecore.Diagnostics;
  using Sitecore.Globalization;
  using Sitecore.Web.UI.Sheer;
  using System;
  using System.Linq;

  public class SelectRenderingDatasourceForm : Sitecore.XA.Foundation.LocalDatasources.Dialogs.SelectRenderingDatasourceForm
  {
    protected override void CreateLocalDatasourceClientPipeline(ClientPipelineArgs args)
    {
      Assert.ArgumentNotNull(args, "args");
      Item item = Client.GetItemNotNull(args.Parameters["itemid"], Language.Parse(args.Parameters["language"]));

      if (args.HasResult && !args.IsPostBack)
      {
        // Let user run the command once again, because he has just saved the changes and the page is now being reloaded
        args.AbortPipeline();
        return;
      }

      if (args.IsPostBack)
      {
        if (!args.HasResult)
        {
          return;
        }

        var itemName = args.Result;

        if (item == null)
        {
          SheerResponse.Alert(Sitecore.Texts.PLEASE_SELECT_AN_ITEM_FIRST, Array.Empty<string>());
        }
        else
        {

          string validationErrorMessage;
          if (!ValidateNewItemName(itemName, out validationErrorMessage))
          {
            SheerResponse.Alert(validationErrorMessage);
          }
          else if (item.Children.Any(child => item.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase)))
          {
            SheerResponse.Alert(Translate.Text(Sitecore.XA.Foundation.LocalDatasources.Texts.ThereIsAlreadyAnItemWithThatName));
          }
          else
          {
            Language contentLanguage = ContentLanguage;
            if (contentLanguage != null && contentLanguage != item.Language)
            {
              item = item.Database.GetItem(item.ID, contentLanguage) ?? item;
            }

            TemplateItem pageDataFolderTemplate = ContentRepository.GetItem(Sitecore.XA.Foundation.LocalDatasources.Items.VirtualPageData);
            if (pageDataFolderTemplate != null && pageDataFolderTemplate.StandardValues != null && item.ID == pageDataFolderTemplate.StandardValues.ID)
            {
              item = CreatePageDataFolder();
            }

            DisableContextsEvents();

            if (Prototype == null)
            {
              FillDatasourceTemplate(item);
            }

            #region Added code
            EnsureHasVersion(item); 
            #endregion

            Item selectedItem = null;

            if (Prototype != null)
            {
              selectedItem = Prototype != null && Prototype.TemplateID == TemplateIDs.BranchTemplate
                  ? item.Add(itemName, (BranchItem)Prototype)
                  : item.Add(itemName, (TemplateItem)Prototype);
            }
            else
            {
              SheerResponse.Alert(Translate.Text(Sitecore.XA.Foundation.LocalDatasources.Texts.RenderingDatasourceTemplateIsNotSet));
              return;
            }


            EnableContextsEvents();

            if (selectedItem != null)
            {
              SetDialogResult(selectedItem);
            }
            SheerResponse.CloseWindow();
          }
        }
      }
      else if (!item.Access.CanCreate())
      {
        SheerResponse.Alert(Sitecore.Texts.YOU_DO_NOT_HAVE_PERMISSION_TO_CREATE_AN_ITEM_HERE);
      }
      else
      {
        SheerResponse.Input(Translate.Text(Sitecore.XA.Foundation.LocalDatasources.Texts.ProvideDatasourceName), string.Empty);
        args.WaitForPostBack();
      }
    }

    #region Added code
    protected virtual void EnsureHasVersion(Item item)
    {
      if (item.Versions.Count == 0)
      {
        item.Versions.AddVersion();
      }
    } 
    #endregion
  }
}