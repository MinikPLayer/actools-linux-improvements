namespace FirstFloor.ModernUI.Presentation {
    public class Displayable : NotifyPropertyChanged {
        /// <summary>
        /// Needed for x:Name in XAML (Avalonia’s compiler wants a Name property on non-control objects).
        /// </summary>
        public string Name { get; set; }

        private string _displayName;

        public virtual string DisplayName {
            get => _displayName;
            set => Apply(value, ref _displayName);
        }
    }
}
