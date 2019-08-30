using System.Windows;

namespace L4d2AddonsMgr {

    /*
     * To murder whitespaces when window width is larger than 3 columns but a bit less than 4.
     * https://stackoverflow.com/questions/8004981/how-to-make-wpf-wrappanel-child-items-to-stretch
     * 
     * No you cannot do this.
     * https://stackoverflow.com/questions/36458766/custom-expression-to-define-width-and-height-in-wpf
     * 
     * How to? Simple.
     * https://stackoverflow.com/questions/6405458/how-do-i-make-custom-controls-in-c
     * 
     * Needs a property for item min width as a default width without stretch.
     * https://stackoverflow.com/questions/3188782/adding-properties-to-custom-wpf-control
     * https://stackoverflow.com/questions/25895011/how-to-add-custom-properties-to-wpf-user-control
     */
    public class MeowWrapPanel : VirtualizingWrapPanel {

        static readonly DependencyProperty ItemMinWidthProperty;

        static MeowWrapPanel() {
            ItemMinWidthProperty = DependencyProperty.Register("ItemMinWidth", typeof(double), typeof(MeowWrapPanel));
        }

        public double ItemMinWidth {
            get => (double)GetValue(ItemMinWidthProperty);
            set => SetValue(ItemMinWidthProperty, value);
        }

        protected override Size MeasureOverride(Size constraint) {

            // Excellent division.
            // https://stackoverflow.com/questions/17944/how-to-round-up-the-result-of-integer-division

            // This is for extending uniformgrid.
            //Columns = (int)Math.Floor(constraint.Width / 200);
            //Rows = (Children.Count + Columns - 1) / Columns;
            //var old = base.MeasureOverride(constraint);
            //return new Size(old.Width, Rows * 120);

            var cols = (int)(constraint.Width / ItemMinWidth);
            ItemWidth = constraint.Width / cols;
            return base.MeasureOverride(constraint);
        }
    }
}
