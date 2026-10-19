using System;
using System.Collections;
using System.Windows.Data;
using JetBrains.Annotations;

namespace FirstFloor.ModernUI {
    /// <summary>
    /// The WPF version fixed some crashes of ListCollectionView, no need for that with Avalonia’s collection view.
    /// </summary>
    public class BetterListCollectionView : ListCollectionView {
        public BetterListCollectionView([NotNull] IList list) : base(list) { }

        public void Refresh([NotNull] object obj) {
            EditItem(obj);
            CommitEdit();
        }
    }
}
