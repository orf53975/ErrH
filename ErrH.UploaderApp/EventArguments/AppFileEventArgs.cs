﻿using System;
using System.Collections.Generic;
using ErrH.UploaderApp.Models;

namespace ErrH.UploaderApp.EventArguments
{
    public class AppFileEventArgs : EventArgs
    {
        public List<AppFile>  List  { get; set; }
        public AppFile        File  { get; set; }
        public AppDir         App   { get; set; }
    }
}
