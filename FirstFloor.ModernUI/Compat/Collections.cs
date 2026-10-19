using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Collections.Specialized;
using System.Globalization;
using Avalonia.Collections;

namespace System.Windows.Data {
    /// <summary>
    /// WPF’s ListCollectionView on top of Avalonia’s DataGridCollectionView (filtering, sorting, grouping, current item),
    /// which is sealed, so everything is forwarded to it.
    /// </summary>
    public class ListCollectionView : IDataGridCollectionView, IList, INotifyCollectionChanged, INotifyPropertyChanged {
        private readonly DataGridCollectionView _inner;

        public ListCollectionView(IList list) {
            _inner = new DataGridCollectionView(list);
        }

        private IComparer _customSort;

        public IComparer CustomSort {
            get => _customSort;
            set {
                _customSort = value;
                using (DeferRefresh()) {
                    SortDescriptions.Clear();
                    if (value != null) {
                        SortDescriptions.Add(DataGridSortDescription.FromComparer(value));
                    }
                }
            }
        }

        public Func<object, bool> Filter {
            get => _inner.Filter;
            set => _inner.Filter = value;
        }

        public Avalonia.Collections.AvaloniaList<DataGridGroupDescription> GroupDescriptions => _inner.GroupDescriptions;

        public CultureInfo Culture { get => _inner.Culture; set => _inner.Culture = value; }
        public IEnumerable SourceCollection => _inner.SourceCollection;
        public bool CanFilter => _inner.CanFilter;
        public DataGridSortDescriptionCollection SortDescriptions => _inner.SortDescriptions;
        public bool CanSort => _inner.CanSort;
        public bool CanGroup => _inner.CanGroup;
        public bool IsGrouping => ((IDataGridCollectionView)_inner).IsGrouping;
        public int GroupingDepth => ((IDataGridCollectionView)_inner).GroupingDepth;
        public string GetGroupingPropertyNameAtDepth(int level) => ((IDataGridCollectionView)_inner).GetGroupingPropertyNameAtDepth(level);
        public IAvaloniaReadOnlyList<object> Groups => _inner.Groups;
        public bool IsEmpty => _inner.IsEmpty;
        public void Refresh() => _inner.Refresh();
        public IDisposable DeferRefresh() => _inner.DeferRefresh();
        public object CurrentItem => _inner.CurrentItem;
        public int CurrentPosition => _inner.CurrentPosition;
        public bool IsCurrentAfterLast => _inner.IsCurrentAfterLast;
        public bool IsCurrentBeforeFirst => _inner.IsCurrentBeforeFirst;
        public bool MoveCurrentToFirst() => _inner.MoveCurrentToFirst();
        public bool MoveCurrentToLast() => _inner.MoveCurrentToLast();
        public bool MoveCurrentToNext() => _inner.MoveCurrentToNext();
        public bool MoveCurrentToPrevious() => _inner.MoveCurrentToPrevious();
        public bool MoveCurrentTo(object item) => _inner.MoveCurrentTo(item);
        public bool MoveCurrentToPosition(int position) => _inner.MoveCurrentToPosition(position);

        public event EventHandler<DataGridCurrentChangingEventArgs> CurrentChanging {
            add => _inner.CurrentChanging += value;
            remove => _inner.CurrentChanging -= value;
        }

        public event EventHandler CurrentChanged {
            add => _inner.CurrentChanged += value;
            remove => _inner.CurrentChanged -= value;
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged {
            add => ((INotifyCollectionChanged)_inner).CollectionChanged += value;
            remove => ((INotifyCollectionChanged)_inner).CollectionChanged -= value;
        }

        public event PropertyChangedEventHandler PropertyChanged {
            add => ((INotifyPropertyChanged)_inner).PropertyChanged += value;
            remove => ((INotifyPropertyChanged)_inner).PropertyChanged -= value;
        }

        // editing
        public bool CanAddNew => _inner.CanAddNew;
        public bool CanCancelEdit => _inner.CanCancelEdit;
        public bool IsAddingNew => _inner.IsAddingNew;
        public bool IsEditingItem => _inner.IsEditingItem;
        public object CurrentAddItem => _inner.CurrentAddItem;
        public object CurrentEditItem => _inner.CurrentEditItem;
        public object AddNew() => _inner.AddNew();
        public void CancelEdit() => _inner.CancelEdit();
        public void CancelNew() => _inner.CancelNew();
        public void CommitEdit() => _inner.CommitEdit();
        public void CommitNew() => _inner.CommitNew();
        public void EditItem(object item) => _inner.EditItem(item);
        public bool Contains(object item) => _inner.Contains(item);

        // list
        public int Count => _inner.Count;
        public bool IsFixedSize => true;
        public bool IsReadOnly => true;
        public bool IsSynchronized => false;
        public object SyncRoot => this;

        public object this[int index] {
            get => _inner.GetItemAt(index);
            set => throw new NotSupportedException();
        }

        public object GetItemAt(int index) => _inner.GetItemAt(index);
        public int IndexOf(object item) => _inner.IndexOf(item);
        public IEnumerator GetEnumerator() => _inner.GetEnumerator();
        public void CopyTo(Array array, int index) => ((ICollection)_inner).CopyTo(array, index);
        int IList.Add(object value) => throw new NotSupportedException();
        void IList.Clear() => throw new NotSupportedException();
        void IList.Insert(int index, object value) => throw new NotSupportedException();
        void IList.Remove(object value) => throw new NotSupportedException();
        void IList.RemoveAt(int index) => throw new NotSupportedException();
    }

    public class PropertyGroupDescription : DataGridPathGroupDescription {
        private readonly IValueConverter _converter;

        public PropertyGroupDescription(string propertyName) : base(propertyName) { }

        public PropertyGroupDescription(string propertyName, IValueConverter converter) : base(propertyName) {
            _converter = converter;
        }

        public override object GroupKeyFromItem(object item, int level, System.Globalization.CultureInfo culture) {
            var value = base.GroupKeyFromItem(item, level, culture);
            return _converter == null ? value : _converter.Convert(value, typeof(object), null, culture);
        }
    }

    public static class CollectionViewSource {
        private static readonly ConditionalWeakTable<object, ListCollectionView> Views = new ConditionalWeakTable<object, ListCollectionView>();

        public static ListCollectionView GetDefaultView(object source) {
            return Views.GetValue(source, s => new ListCollectionView((IList)s));
        }
    }

    public static class SortDescriptionExtension {
        public static void Add(this DataGridSortDescriptionCollection collection, System.ComponentModel.SortDescription description) {
            collection.Add(description.Direction == ListSortDirection.Ascending
                    ? DataGridSortDescription.FromPath(description.PropertyName, ListSortDirection.Ascending)
                    : DataGridSortDescription.FromPath(description.PropertyName, ListSortDirection.Descending));
        }
    }
}

namespace System.ComponentModel {
    public struct SortDescription {
        public SortDescription(string propertyName, ListSortDirection direction) {
            PropertyName = propertyName;
            Direction = direction;
        }

        public string PropertyName { get; set; }
        public ListSortDirection Direction { get; set; }
    }
}
