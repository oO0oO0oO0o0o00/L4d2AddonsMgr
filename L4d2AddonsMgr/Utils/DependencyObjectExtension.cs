using System.Windows;
using System.Windows.Media;

namespace L4d2AddonsMgr.Utils {
    public static class DependencyObjectExtension {

        public static T FindAnchestor<T>(this DependencyObject current)
    where T : DependencyObject {
            do {
                if (current is T) {
                    return (T)current;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            while (current != null);
            return null;
        }
    }
}
