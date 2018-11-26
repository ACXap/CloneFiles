using GalaSoft.MvvmLight.Messaging;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CloneFile
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Messenger.Default.Register<NotificationMessage>(this, OpenCloseExpander);
        }

        private void OpenCloseExpander(NotificationMessage obj)
        {
            if (obj.Notification == "expander")
            {
                var g = (string)obj.Target;
               // var g0 = ((string)obj.Target)[0];
                var datagrid = dg;
                if(g[1].Equals('1'))
                {
                    datagrid = dg2;
                }
                FindChild(datagrid, typeof(Expander), g[0].Equals('1'));
            }
        }

        public void FindChild(DependencyObject o, Type childType, bool isExp)
        {
            DependencyObject foundChild = null;
            if (o != null)
            {
                int childrenCount = VisualTreeHelper.GetChildrenCount(o);
                for (int i = 0; i < childrenCount; i++)
                {
                    var child = VisualTreeHelper.GetChild(o, i);
                    if (child.GetType() != childType)
                    {
                        FindChild(child, childType, isExp);
                    }
                    else
                    {
                        foundChild = child;
                        var a = (Expander)foundChild;
                        a.IsExpanded = isExp;
                        break;
                    }
                }
            }
        }
    }
}
