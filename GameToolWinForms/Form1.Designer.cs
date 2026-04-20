namespace GameToolWinForms;

partial class Form1
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
        toolStrip1 = new ToolStrip();

        ListModsButton = new ToolStripButton();
        SearchModLibraryButton = new ToolStripButton();
        listView1 = new ListView();
        columnHeader1 = new ColumnHeader();
        columnHeader2 = new ColumnHeader();
        columnHeader3 = new ColumnHeader();
        columnHeader4 = new ColumnHeader();
        columnHeader5 = new ColumnHeader();
        statusStrip1 = new StatusStrip();
        ModCountTextBlock = new ToolStripStatusLabel();
        OpenDownloadDirButton = new ToolStripButton();
        toolStrip1.SuspendLayout();
        statusStrip1.SuspendLayout();
        SuspendLayout();
        // 
        // toolStrip1
        // 
        toolStrip1.Items.AddRange(new ToolStripItem[] { ListModsButton, SearchModLibraryButton });
        toolStrip1.Location = new Point(0, 0);
        toolStrip1.Name = "toolStrip1";
        toolStrip1.Size = new Size(1000, 25);
        toolStrip1.TabIndex = 0;
        toolStrip1.Text = "toolStrip1";
        // 
        // ListModsButton
        // 
        ListModsButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        ListModsButton.Name = "ListModsButton";
        ListModsButton.Size = new Size(72, 22);
        ListModsButton.Text = "我的修改器";
        ListModsButton.Click += ListModsButton_Click;
        // 
        // SearchModLibraryButton
        // 
        SearchModLibraryButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        SearchModLibraryButton.Name = "SearchModLibraryButton";
        SearchModLibraryButton.Size = new Size(60, 22);
        SearchModLibraryButton.Text = "修改器库";
        SearchModLibraryButton.Click += SearchModLibraryButton_Click;
        // 
        // listView1
        // 
        listView1.Columns.AddRange(new ColumnHeader[] { columnHeader1, columnHeader2, columnHeader3, columnHeader4, columnHeader5 });
        listView1.FullRowSelect = true;
        listView1.Location = new Point(10, 30);
        listView1.Name = "listView1";
        listView1.Size = new Size(980, 500);
        listView1.TabIndex = 1;
        listView1.UseCompatibleStateImageBehavior = false;
        listView1.View = View.Details;
        // 
        // columnHeader1
        // 
        columnHeader1.Text = "ID";
        columnHeader1.Width = 50;
        // 
        // columnHeader2
        // 
        columnHeader2.Text = "名称";
        columnHeader2.Width = 200;
        // 
        // columnHeader3
        // 
        columnHeader3.Text = "游戏";
        columnHeader3.Width = 150;
        // 
        // columnHeader4
        // 
        columnHeader4.Text = "状态";
        columnHeader4.Width = 80;
        // 
        // columnHeader5
        // 
        columnHeader5.Text = "添加日期";
        columnHeader5.Width = 100;
        // 
        // statusStrip1
        // 
        statusStrip1.Items.AddRange(new ToolStripItem[] { ModCountTextBlock, OpenDownloadDirButton });
        statusStrip1.Location = new Point(0, 540);
        statusStrip1.Name = "statusStrip1";
        statusStrip1.Size = new Size(1000, 22);
        statusStrip1.TabIndex = 2;
        statusStrip1.Text = "statusStrip1";
        // 
        // ModCountTextBlock
        // 
        ModCountTextBlock.Name = "ModCountTextBlock";
        ModCountTextBlock.Size = new Size(82, 17);
        ModCountTextBlock.Text = "修改器数量: 0";
        // 
        // OpenDownloadDirButton
        // 
        OpenDownloadDirButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        OpenDownloadDirButton.Name = "OpenDownloadDirButton";
        OpenDownloadDirButton.Size = new Size(100, 20);
        OpenDownloadDirButton.Text = "打开修改器文件夹";
        OpenDownloadDirButton.Click += OpenDownloadDirButton_Click;
        // 
        // Form1
        // 
        AutoScaleDimensions = new SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1000, 562);
        Controls.Add(statusStrip1);
        Controls.Add(listView1);
        Controls.Add(toolStrip1);
        Name = "Form1";
        Text = "游戏修改器下载工具";
        Load += Form1_Load;
        toolStrip1.ResumeLayout(false);
        toolStrip1.PerformLayout();
        statusStrip1.ResumeLayout(false);
        statusStrip1.PerformLayout();
        ResumeLayout(false);
        PerformLayout();

    }

    #endregion
}
