﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ErrH.Tools.Extensions;
using ErrH.WpfTools.Extensions;
using ErrH.WpfTools.ViewModels;

namespace ErrH.WpfTools.UserControls
{
    /// <summary>
    /// Interaction logic for UserSessionMenu.xaml
    /// </summary>
    public partial class UserSessionMenu : UserControl
    {
        public UserSessionMenu()
        {
            InitializeComponent();

            Loaded += (s, e) 
                => tbxPwd.DoLogin(btnLogin.Command, lblPwd);
        }
    }
}
