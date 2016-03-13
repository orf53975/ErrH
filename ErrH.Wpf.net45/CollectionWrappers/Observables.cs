﻿using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ErrH.Wpf.net45.CollectionWrappers
{
    public class Observables<T> : ObservableCollection<T>
    {
        public Observables() { }
        public Observables(IEnumerable<T> list) : base(list) { }


        public void Add(IEnumerable<T> items)
        {
            if (items == null) return;
            foreach (var item in items) Add(item);
        }


        /// <summary>
        /// Clears all existing items, then adds the new ones.
        /// </summary>
        /// <param name="items"></param>
        public void Swap(IEnumerable<T> items)
        {
            Clear();
            Add(items);
        }
    }
}
