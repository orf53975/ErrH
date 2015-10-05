﻿using ErrH.BinUpdater.Core.Configuration;
using ErrH.Tools.Authentication;
using ErrH.Uploader.ViewModels.ContentVMs;
using ErrH.Uploader.ViewModels.NavigationVMs;
using ErrH.WpfTools.ViewModels;

namespace ErrH.Uploader.ViewModels
{

    public class MainWindowVM : MainWindowVmBase
    {




        public MainWindowVM(IConfigFile cfgFile, ISessionClient d7Client)
        {
            DisplayName   = "ErrH Uploader (2nd attempt)";

            OtherTabs.Add(new LogScrollerVM(this));

            UserSession.SetClient(d7Client);

            cfgFile.CredentialsReady += (s, e) =>
                { UserSession.Credentials = e.Value; };

            CompletelyLoaded += (src, ea) =>
            {
                var foldrsTab = ForwardLogs(IoC.Resolve<FoldersTabVM>());

                foldrsTab.MainList.ItemPicked += (s, e) =>
                    { ShowSingleton<FilesTabVM2>(e.Value, IoC); };

                NaviTabs.Add(foldrsTab);
                NaviTabs.SelectOne(0);
                foldrsTab.Refresh();
            };
        }


        //protected override void OnRefresh()
        //{
        //    base.OnRefresh();
        //    //NaviTabs.SelectOne(0);
        //}

    }
}