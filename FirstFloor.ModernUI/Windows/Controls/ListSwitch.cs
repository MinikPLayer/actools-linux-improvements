using System;
using System.Collections;
using System.Linq;
using System.Windows;
using System.Windows.Markup;

namespace FirstFloor.ModernUI.Windows.Controls {
    public abstract class ListSwitch : BaseSwitch, IList {
        [Avalonia.Metadata.Content]
        public IList Children => this;

        public void CopyTo(Array array, int index) {
            ((IList)RegisteredElements).CopyTo(array, index);
        }

        public virtual int Count => RegisteredElements.Count;

        public object SyncRoot => ((IList)RegisteredElements).SyncRoot;

        public bool IsSynchronized => ((IList)RegisteredElements).IsSynchronized;

        public void RemoveAt(int index) {
            RegisterChild(RegisteredElements[index], null);
        }

        public UIElement this[int index] {
            get => RegisteredElements[index];
            set => RegisterChild(RegisteredElements[index], value);
        }

        public bool Contains(UIElement element) {
            return RegisteredElements.Contains(element);
        }

        public virtual void Clear() {
            ClearRegisteredChildren();
        }

        public int Add(UIElement element) {
            InvalidateMeasure();
            RegisterChild(null, element);
            return RegisteredElements.Count;
        }

        public void Insert(int index, UIElement element) {
            InvalidateMeasure();
            RegisterChild(null, element);
        }

        public void Remove(UIElement element) {
            RegisterChild(element, null);
        }

        int IList.Add(object value) {
            return Add((UIElement)value);
        }

        bool IList.Contains(object value) {
            return Contains(value as UIElement);
        }

        int IList.IndexOf(object value) {
            return IndexOf(value as UIElement);
        }

        void IList.Insert(int index, object value) {
            Insert(index, (UIElement)value);
        }

        bool IList.IsFixedSize => false;

        bool IList.IsReadOnly => false;

        void IList.Remove(object value) {
            Remove(value as UIElement);
        }

        object IList.this[int index] {
            get => this[index];
            set => this[index] = (UIElement)value;
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return ((IEnumerable)RegisteredElements).GetEnumerator();
        }

        protected abstract bool TestChild(UIElement child);

        protected override UIElement GetChild() {
            return RegisteredElements?.FirstOrDefault(TestChild);
        }
    }
}