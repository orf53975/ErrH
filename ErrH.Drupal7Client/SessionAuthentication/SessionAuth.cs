﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErrH.RestSharpShim;
using ErrH.Tools.Drupal7Models.DTOs;
using ErrH.Tools.Extensions;
using ErrH.Tools.FileSystemShims;
using ErrH.Tools.Loggers;
using ErrH.Tools.RestServiceShim;
using ErrH.Tools.Serialization;

namespace ErrH.Drupal7Client.SessionAuthentication
{
    internal class SessionAuth : LogSourceBase
    {
        internal string ParentDir => "D7Client";

        private IFileSystemShim _fs;
        private ISerializer     _serialzr;

        internal bool   _isSessionValid = false;
        private string _userName;
        private string _password;
        

        internal D7UserSession Current     { get; set; }
        internal FileShim      SessionFile { get; set; }



        internal RequestFactory Req
            => new RequestFactory(this.Current, _userName, _password);

        //internal bool IsLoggedIn => this.Current != null;
        internal bool IsLoggedIn => _isSessionValid;


        public SessionAuth(IFileSystemShim fsShim, ISerializer serializer)
        {
            _fs         = fsShim;
            _serialzr   = serializer;
            var dir     = _fs.GetSpecialDir(SpecialDir.LocalApplicationData)
                                                        .Bslash(ParentDir);
            SessionFile = _fs.File(dir.Bslash("d7.session"));
        }


        internal D7UserSession ReadSessionFile()
        {
            if (!SessionFile._Found) return null;
            return _serialzr.Read<D7UserSession>(SessionFile, false);
        }


        internal void WriteSessionFile()
        {
            var contnt = _serialzr.Write(Current, false);
            if (!SessionFile.Write(contnt, EncodeAs.UTF8)) return;
            SessionFile.Hidden = true;
        }


        internal async Task<bool> OpenNewSession(IClientShim client, string userName, string password, CancellationToken cancelToken)
        {
            var tokn = await GetToken(client, cancelToken);
            if (tokn.IsBlank()) return false;

            var req       = RequestShim.POST(URL.Api_UserLogin);
            req.UserName  = _userName = userName;
            req.Password  = _password = Saltify(password);
            req.CsrfToken = tokn;

            var user = await ValidateCredentials(client, req, cancelToken);
            if (user == null) return false;

            this.Current = await GetUserSession(client, user, cancelToken);
            if (this.Current == null) return false;
            return _isSessionValid = true;
        }





        private async Task<D7UserSession> ValidateCredentials(IClientShim client, RequestShim req, CancellationToken cancelToken)
        {
            req.Body = new { username = _userName, password = _password };
            req.UserName = _userName;
            req.Password = _password;
            D7UserSession ret = null;

            try {
                ret = await client.Send<D7UserSession>(req, cancelToken);
            }
            catch (Exception ex) {
                LogError("client.Send<D7UserSession>", ex);
                return null;
            }
            return ret;
        }


        private string Saltify(string visiblePwd)
        {
            if (visiblePwd.Length != 40) return visiblePwd;
            if (!visiblePwd.IsAllLower()) return visiblePwd;
            return visiblePwd.ToUpper().Repeat(3).SHA1();
        }




        private async Task<D7UserSession> GetUserSession(IClientShim client, D7UserSession usr, CancellationToken cancelToken)
        {
            var req = RequestFactory.Make(URL.Api_SystemConnect, RestMethod.Post, usr, _userName, _password);
            req.UserName = _userName;
            req.Password = _password;

            var sess = await client.Send<D7UserSession>(req, cancelToken);
            sess.token = usr.token;
            sess.BaseURL = client.BaseUrl;
            return sess;
        }


        private async Task<string> GetToken(IClientShim client, CancellationToken cancelToken)
        {
            var req = RequestShim.POST(URL.Api_UserToken);
            D7SessionToken tok;
            try {
                tok = await client.Send<D7SessionToken>(req, cancelToken);
            }
            catch (Exception ex)
            {
                LogError("client.Send", ex);
                return null;
            }
            return tok?.token;
        }


        internal async Task<bool> CloseSession(IClientShim client, CancellationToken cancelToken)
        {
            Debug_n("Closing user session...", "");
            var req = Req.POST(URL.Api_UserLogout);
            var resp = await client.Send<List<bool>>(req, cancelToken);
            var ok = resp.FirstOrDefault();

            this.Current = null;

            if (ok) return Debug_n("Session successfully closed.", "");
            else return Warn_n("Unexpected logout reply.", ok);
        }








    }
}
