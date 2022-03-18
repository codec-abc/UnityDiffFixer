using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DiffPlex.Wpf;
using DiffPlex.Model;
using System.IO;

namespace UnityDiffFixer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public static Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            string before = Encoding.UTF8.GetString(UnityDiffFixerWPF.Properties.Resources.old_scene);
            string after = Encoding.UTF8.GetString(UnityDiffFixerWPF.Properties.Resources.new_scene);

            var oldLines = StringUtils.GetAllLinesFromText(before);
            var oldDocument = UnityYAMLDocument.ParseUnityYAMLdocument(oldLines);

            var newLines = StringUtils.GetAllLinesFromText(after);
            var newDocument = UnityYAMLDocument.ParseUnityYAMLdocument(newLines);

            var comparerAndFixer = new UnityDiffComparerAndFixer(oldDocument, newDocument);
            var fixedYaml = comparerAndFixer.GetFixedVersion(a => Console.WriteLine(a));

            File.WriteAllText(@"C:\Users\c.viot\Desktop\old.yml", before);
            File.WriteAllText(@"C:\Users\c.viot\Desktop\new.yml", after);
            File.WriteAllText(@"C:\Users\c.viot\Desktop\fixed.yml", fixedYaml);

            //var diff = InlineDiffBuilder.Diff(before, fixedYaml);
            //DiffView.SetDiffModel(before, after);
            //DiffView.OldText = before;
            //DiffView.NewText = after;
        }
    }
}
