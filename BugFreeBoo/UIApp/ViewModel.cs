using System.Collections.ObjectModel;
using System.Windows;

namespace UIApp
{
    public class ViewModel : DependencyObject
    {
        public ObservableCollection<string> Collection { get; set; }

        public ViewModel()
        {
            Collection = new ObservableCollection<string>() {"eden", "dva", "tri"};
        }
    }
}
