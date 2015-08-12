﻿using System.Collections.Generic;
using ErrH.UploaderApp.Models;

namespace ErrH.UploaderApp.EventArguments
{
    internal class EvtArg
    {

        public static AppFileEventArg AppFile(AppFolder app, List<AppFile> list)
        {
            return new AppFileEventArg { App = app, List = list };
        }


        public static AppFileEventArg AppFile(List<AppFile> list)
        {
            return new AppFileEventArg { List = list };
        }


        public static AppFileEventArg AppFile(AppFile file)
        {
            return new AppFileEventArg { File = file };
        }





        public static AppFolderEventArg AppDir(List<AppFolder> list)
        {
            return new AppFolderEventArg { List = list };
        }

        public static AppFolderEventArg AppDir(AppFolder app)
        {
            return new AppFolderEventArg { App = app };
        }

    }
}
