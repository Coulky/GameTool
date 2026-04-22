namespace GameTrainersManager;

partial class Index
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        listView1 = new ListView();
        columnHeader2 = new ColumnHeader();
        columnHeader8 = new ColumnHeader();
        columnHeader6 = new ColumnHeader();
        columnHeader3 = new ColumnHeader();
        statusStrip1 = new StatusStrip();
        ModCountTextBlock = new ToolStripStatusLabel();
        OpenDownloadDirButton = new ToolStripButton();
        FindModsButton = new ToolStripButton();
        statusStrip1.SuspendLayout();
        SuspendLayout();
        // 
        // OpenDownloadDirButton
        // 
        OpenDownloadDirButton.Alignment = ToolStripItemAlignment.Right;
        OpenDownloadDirButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        OpenDownloadDirButton.Name = "OpenDownloadDirButton";
        OpenDownloadDirButton.Size = new Size(88, 22);
        OpenDownloadDirButton.Text = "本地修改器库";
        OpenDownloadDirButton.Click += OpenDownloadDirButton_Click;
        // 
        // FindModsButton
        // 
        FindModsButton.Alignment = ToolStripItemAlignment.Right;
        FindModsButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        FindModsButton.Name = "FindModsButton";
        FindModsButton.Size = new Size(108, 22);
        FindModsButton.Text = "从远端查找修改器";
        FindModsButton.Click += FindModsButton_Click;
        // 
        // listView1
        // 
        listView1.Columns.AddRange(new ColumnHeader[] { columnHeader2, columnHeader8, columnHeader6, columnHeader3 });
        listView1.FullRowSelect = true;
        listView1.Location = new Point(10, 10);
        listView1.Name = "listView1";
        listView1.Size = new Size(540, 353);
        listView1.TabIndex = 1;
        listView1.UseCompatibleStateImageBehavior = false;
        listView1.View = View.Details;
        // 
        // columnHeader2
        // 
        columnHeader2.Text = "游戏名";
        columnHeader2.Width = 300;
        // 
        // columnHeader8
        // 
        columnHeader8.Text = "启动";
        columnHeader8.Width = 80;
        // 
        // columnHeader6
        // 
        columnHeader6.Text = "更新";
        columnHeader6.Width = 80;
        // 
        // columnHeader3
        // 
        columnHeader3.Text = "删除";
        columnHeader3.Width = 80;
        // 
        // statusStrip1
        // 
        statusStrip1.Items.AddRange(new ToolStripItem[] { ModCountTextBlock, FindModsButton, OpenDownloadDirButton });
        statusStrip1.Location = new Point(0, 363);
        statusStrip1.Name = "statusStrip1";
        statusStrip1.Size = new Size(560, 22);
        statusStrip1.TabIndex = 2;
        statusStrip1.Text = "statusStrip1";
        // 
        // ModCountTextBlock
        // 
        ModCountTextBlock.Name = "ModCountTextBlock";
        ModCountTextBlock.Size = new Size(82, 17);
        ModCountTextBlock.Text = "修改器数量: 0";
        // 
        // Form1
        // 
        AutoScaleDimensions = new SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(560, 385);
        Controls.Add(statusStrip1);
        Controls.Add(listView1);
        Name = "Index";
        Text = "游戏修改器下载工具";
        Load += Index_Load;
        statusStrip1.ResumeLayout(false);
        statusStrip1.PerformLayout();
        ResumeLayout(false);
        PerformLayout();

    }

    #endregion
}
