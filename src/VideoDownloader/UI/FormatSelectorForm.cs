using YtdlpNET;

namespace VideoDownloader.UI;

public partial class FormatSelectorForm : Form
{
    public Format? SelectedFormat { get; private set; }

    public FormatSelectorForm(List<Format> formats)
    {
        ListView listFormats = new();
        listFormats.View = View.Details;
        listFormats.FullRowSelect = true;

        listFormats.Columns.Add("ID", 80);
        listFormats.Columns.Add("Resolution", 120);
        listFormats.Columns.Add("Ext", 80);
        listFormats.Columns.Add("Size", 120);

        foreach (var f in formats)
        {
            var item = new ListViewItem(f.Id);
            item.SubItems.Add(f.Resolution);
            item.SubItems.Add(f.Extension);
            item.SubItems.Add(f.FileSizeApprox);

            item.Tag = f;

            listFormats.Items.Add(item);
        }

        listFormats.DoubleClick += (_, _) =>
        {
            if (listFormats.SelectedItems.Count == 0)
                return;

            SelectedFormat =
                listFormats.SelectedItems[0].Tag as Format;

            DialogResult = DialogResult.OK;
        };
    }
}