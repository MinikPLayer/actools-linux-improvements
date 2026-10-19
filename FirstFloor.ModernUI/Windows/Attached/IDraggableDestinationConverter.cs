using JetBrains.Annotations;

namespace FirstFloor.ModernUI.Windows.Attached {
    public interface IDraggableDestinationConverter {
        [CanBeNull]
        object Convert([NotNull] IDataObject data);
    }
}