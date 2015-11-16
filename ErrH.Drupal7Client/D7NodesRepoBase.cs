﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ErrH.Tools.Authentication;
using ErrH.Tools.CollectionShims;
using ErrH.Tools.Drupal7Models;
using ErrH.Tools.Drupal7Models.Entities;
using ErrH.Tools.Drupal7Models.FieldAttributes;
using ErrH.Tools.Drupal7Models.Fields;
using ErrH.Tools.ErrorConstructors;
using ErrH.Tools.Extensions;

namespace ErrH.Drupal7Client
{
    public abstract class D7NodesRepoBase<TNodeDto, TClass> 
        : ListRepoBase<TClass> 
        where TClass : ID7Node
    {
        const int RETRY_INTERVAL_SEC = 5;

        protected ID7Client               _client;
        protected IBasicAuthenticationKey _credentials;

        public override ISessionClient Client => _client;
        public override IBasicAuthenticationKey AuthKey => _credentials;

        protected abstract TClass FromDto(TNodeDto dto);



        public override bool Add(TClass itemToAdd)
        {
            if (!base.Add(itemToAdd)) return false;

            NewUnsavedItems.Add(itemToAdd);

            return true;
        }


        public override void SetClient(ISessionClient sessionClient, IBasicAuthenticationKey credentials)
        {
            _client = sessionClient.As<ID7Client>();
            _credentials = credentials;
        }

        public override void ShareClientWith<TAny>(IRepository<TAny> anotherRepo)
            => anotherRepo.SetClient(_client, _credentials);


        //public override bool Add(TClass itemToAdd)
        //{
        //    var valid = base.Add(itemToAdd);
        //    if (valid) _newItems.Add(itemToAdd);
        //    return valid;
        //}



        public override async Task<bool> LoadAsync(CancellationToken tkn, params object[] args)
        {
            //if (args == null || args.Length != 2)
            //    Throw.BadArg(nameof(args), "should have 2 items");
            //var rsrc = args[0].ToString().Slash(args[1]);
            var rsrc = string.Join("/", args);


            //if (!_client.IsLoggedIn) return Error_n(
            //    "Currently disconnected from data source.",
            //        "Call Connect() before Load().");                

            //Cancelled += (s, e) => { cancelSrc.Cancel(); };
            //return await TryAndTry(rsrc, cancelSrc.Token);
            return await DoActualLoad(rsrc, tkn);
        }


        private async Task<bool> TryAndTry
            (string resourceUrl, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (await DoActualLoad(resourceUrl, token)) return true;

                Warn_n("Failed to load remote data.", 
                      $"Retrying after {RETRY_INTERVAL_SEC} seconds...");

                await DelayRetry(RETRY_INTERVAL_SEC, token);
            }
            return false;
        }


        private async Task DelayRetry
            (int seconds, CancellationToken token)
        {
            for (int i = seconds; i > 0; i--)
            {
                RaiseDelayingRetry(i);

                try   { await TaskEx.Delay(1000, token); }
                catch { return; }
            }
        }



        private async Task<bool> DoActualLoad(string rsrc, CancellationToken cancelToken)
        {
            RaiseLoading();

            Debug_n("Loading repository data from source...", rsrc);

            if (!_credentials.UserName.IsBlank() 
              && _credentials.Password.IsBlank())
                return Warn_n("LoginCfgFile does not include a password.",
                              "Please supply a password to login.");

            //if (!_credentials.IsCompleteInfo)
            //    return Error_n("LoginCfgFile may not have been parsed.", 
            //                   "_credentials.IsCompleteInfo == FALSE");

            if (!_client.LocalizeSessionFile(_credentials)) return false;

            if (!_client.IsLoggedIn)
                if (_client.HasSavedSession) _client.LoadSession();

            if (!_client.IsLoggedIn)
                if (!await _client.Login(_credentials, cancelToken)) return false;


            var dtos = await _client.Get<List<TNodeDto>>(rsrc, cancelToken, null,
                        "Successfully loaded repository data ({0} fetched).",
                            x => "{0:record}".f(x.Count));

            if (dtos == null) return false;

            _list = dtos.Select(x => FromDto(x)).ToList();

            if (_list.Count == 1 && _list[0] == null) _list.Clear();

            StartTrackingChanges();

            RaiseLoaded();
            return true;
        }


        protected void StartTrackingChanges()
        {
            foreach (var item in _list)
            {
                item.PropertyChanged += (s, e) => 
                {
                    if (!ChangedUnsavedItems.Has(x => x.nid == item.nid))
                        ChangedUnsavedItems.Add(item);
                };
            }
        }



        protected bool AreKeysUnique(IEnumerable<TClass> list)
        {
            var grpd = list.GroupBy(this.GetKey);
            if (grpd.Count() == list.Count()) return true;

            foreach (var grp in grpd.Where(x => x.Count() > 1))
                Warn_n($"Found non-unique ‹{typeof(TClass).Name}› key.", 
                       $"Key “{grp.Key}” used by {grp.Count().x("record")}");

            return false;
        }


        protected override List<TClass> LoadList(object[] args)
        {
            throw Error.BadAct("Implementations of D7NodesRepoBase<T> should call LoadAsync() instead of Load().");
        }






        public async override Task<bool> SaveChangesAsync(CancellationToken tkn = default(CancellationToken))
        {
            foreach (var item in NewUnsavedItems)
            {
                var dto = ToApiDto(item);
                if (dto == null) return false;
                var node = await _client.Post(dto, tkn);
                if (node == null || node.nid < 1) return false;
            }
            NewUnsavedItems.Clear();

            foreach (var item in ChangedUnsavedItems)
            {
                var dto = ToApiDto(item) as ID7NodeRevision;
                if (dto == null) return false;
                dto.nid = item.nid;
                dto.vid = ((ID7NodeRevision)item).vid;
                //var node = await _client.Put(dto, tkn);
                //if (node == null || node.nid < 1) return false;
                if (!await _client.Put(dto, tkn)) return false;
            }
            ChangedUnsavedItems.Clear();

            return true;
        }



        private D7NodeBase ToApiDto(TClass item)
        {
            var typIn = item.GetType();
            var dtoAtt = typIn.GetAttribute<D7NodeDtoAttribute>
                                          (errofIfMissing: true);
            var typOut = dtoAtt.DtoType;
            var nodeOut = Activator.CreateInstance(typOut) as D7NodeBase;

            nodeOut.type = dtoAtt.MachineName;

            foreach (var inProp in typIn.PublicInstanceProps())
            {
                var att = inProp.GetAttribute<D7FieldAttribute>(true);
                if (att != null)
                {
                    var val = inProp.GetValue(item, null);
                    var outProp = typOut.GetProperty(att.FieldName);
                    SetFieldValue(nodeOut, outProp, att, val);
                }
            }
            return nodeOut.As<D7NodeBase>();
        }


        //private ID7NodeRevision ToApiDtoRevision(TClass item)
        //{

        //}


        private void SetFieldValue(D7NodeBase d7Node, PropertyInfo prop,
                                   D7FieldAttribute d7fieldAttrib, object value)
        {
            if (prop == null) return;
            object fieldVal = null;
            switch (d7fieldAttrib.FieldType)
            {
                case D7FieldTypes.DirectValue:
                    fieldVal = value;
                    break;

                case D7FieldTypes.CckField:
                    fieldVal = und.Values(value);
                    break;

                case D7FieldTypes.NodeReference:
                    fieldVal = und.TargetIds(value.ToString().ToInt());
                    break;

                case D7FieldTypes.FileReference:
                    fieldVal = und.Fids(value.ToString().ToInt());
                    break;

                default:
                    throw Error.Unsupported(d7fieldAttrib.FieldType);
            }
            prop.SetValue(d7Node, fieldVal, null);
        }


    }
}
