using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AcManager.Controls.ViewModels;
using AcManager.Tools.Helpers;
using AcManager.Tools.Objects;
using FirstFloor.ModernUI.Windows;
using FirstFloor.ModernUI.Windows.Attached;
using FirstFloor.ModernUI.Windows.Media;

namespace AcManager.Controls {
    public class RaceGridEditorTable : Control {
        static RaceGridEditorTable() {
        }

        public static readonly StyledProperty<RaceGridViewModel> ModelProperty = DP.Register<RaceGridViewModel>(nameof(Model),
                typeof(RaceGridEditorTable), new PropertyMetadata(null, (o, e) => {
                    var t = (RaceGridEditorTable)o;
                    t._model = (RaceGridViewModel)e.NewValue;
                    t.UpdateConditions();
                }));

        private RaceGridViewModel _model;

        public RaceGridViewModel Model {
            get => _model;
            set => SetValue(ModelProperty, value);
        }

        public static readonly StyledProperty<ICommand> CloseCommandProperty = DP.Register<ICommand>(nameof(CloseCommand),
                typeof(RaceGridEditorTable), new PropertyMetadata(null, (o, e) => {
                    ((RaceGridEditorTable)o)._closeCommand = (ICommand)e.NewValue;
                }));

        private ICommand _closeCommand;

        public ICommand CloseCommand {
            get => _closeCommand;
            set => SetValue(CloseCommandProperty, value);
        }

        public static readonly StyledProperty<ICommand> AddOpponentCommandProperty = DP.Register<ICommand>(nameof(AddOpponentCommand),
                typeof(RaceGridEditorTable), new PropertyMetadata(null, (o, e) => {
                    ((RaceGridEditorTable)o)._addOpponentCommand = (ICommand)e.NewValue;
                }));

        private ICommand _addOpponentCommand;

        public ICommand AddOpponentCommand {
            get => _addOpponentCommand;
            set => SetValue(AddOpponentCommandProperty, value);
        }

        private DataGrid _dataGrid;
        private readonly SizeRelatedCondition[] _sizeRelated;

        public RaceGridEditorTable() {
            _sizeRelated = new SizeRelatedCondition[] {
                this.AddWidthCondition(640).Add(t => this.GetTemplateChild(@"PART_NameColumn") as DataGridColumn),
                this.AddWidthCondition(740).Add(t => this.GetTemplateChild(@"PART_BallastColumn") as DataGridColumn),
                this.AddWidthCondition(840).Add(t => this.GetTemplateChild(@"PART_RestrictorColumn") as DataGridColumn),
                this.AddWidthCondition(980).Add(t => this.GetTemplateChild(@"PART_NationalityColumn") as DataGridColumn),

                SettingsHolder.Drive.QuickDriveAiLimitations ?
                        this.AddWidthCondition(1140).Add(t => this.GetTemplateChild(@"PART_AiLimitationColumn") as DataGridColumn) : null,

                this.AddSizeCondition(p => p.ActualWidth >= 740 && p._model.PlayerCar != null).Add(
                        t => this.GetTemplateChild(@"PART_PlayerBallast") as FrameworkElement),
                this.AddSizeCondition(p => p.ActualWidth >= 840 && p._model.PlayerCar != null).Add(
                        t => this.GetTemplateChild(@"PART_PlayerRestrictor") as FrameworkElement)
            };
        }

        private void UpdateConditions() {
            foreach (var condition in _sizeRelated) {
                condition?.Update();
            }
        }

        protected override void OnApplyTemplate(Avalonia.Controls.Primitives.TemplateAppliedEventArgs e) {
            this.RememberTemplateScope(e);
            if (_dataGrid != null) {
                _dataGrid.RemoveDrop(OnDrop);
            }

            base.OnApplyTemplate(e);

            _dataGrid = this.GetTemplateChild(@"PART_DataGrid") as DataGrid;

            if (_dataGrid != null) {
                _dataGrid.AddDrop(OnDrop);
            }

            UpdateConditions();
        }

        private void OnDrop(object sender, DragEventArgs e) {
            var raceGridEntry = e.Data.GetData(RaceGridEntry.DraggableFormat) as RaceGridEntry;
            var carObject = e.Data.GetData(CarObject.DraggableFormat) as CarObject;

            if (raceGridEntry == null && carObject == null || Model == null) {
                e.Effects = DragDropEffects.None;
                return;
            }

            var newIndex = ((ItemsControl)sender).GetMouseItemIndex();
            if (raceGridEntry != null) {
                Model.InsertEntry(newIndex, e.IsCopyAction() ? raceGridEntry.Clone() : raceGridEntry);
            } else {
                Model.InsertEntry(newIndex, carObject);
            }

            e.Effects = DragDropEffects.Move;
        }
    }
}