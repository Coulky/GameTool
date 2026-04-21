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
        columnHeader2 = new ColumnHeader();
        columnHeader5 = new ColumnHeader();
        columnHeader8 = new ColumnHeader();
        columnHeader6 = new ColumnHeader();
        columnHeader7 = new ColumnHeader();
        statusStrip1 = new StatusStrip();
        ModCountTextBlock = new ToolStripStatusLabel();
        OpenDownloadDirButton = new ToolStripButton();
        searchPanel = new Panel();
        btnSearch = new Button();
        txtSearch = new TextBox();
        alphabetPanel = new FlowLayoutPanel();
        toolStrip1.SuspendLayout();
        statusStrip1.SuspendLayout();
        searchPanel.SuspendLayout();
        SuspendLayout();
        // 
        // toolStrip1
        // 
        toolStrip1.Items.AddRange(new ToolStripItem[] { ListModsButton, SearchModLibraryButton, OpenDownloadDirButton });
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
        // searchPanel
        // 
        searchPanel.Controls.Add(btnSearch);
        searchPanel.Controls.Add(txtSearch);
        searchPanel.Location = new Point(10, 30);
        searchPanel.Name = "searchPanel";
        searchPanel.Size = new Size(980, 40);
        searchPanel.TabIndex = 3;
        // 
        // btnSearch
        // 
        btnSearch.Location = new Point(880, 5);
        btnSearch.Name = "btnSearch";
        btnSearch.Size = new Size(90, 30);
        btnSearch.TabIndex = 1;
        btnSearch.Text = "搜索";
        btnSearch.UseVisualStyleBackColor = true;
        // 
        // txtSearch
        // 
        txtSearch.Location = new Point(10, 5);
        txtSearch.Name = "txtSearch";
        txtSearch.Size = new Size(860, 23);
        txtSearch.TabIndex = 0;
        // 
        // alphabetPanel
        // 
        alphabetPanel.Location = new Point(10, 75);
        alphabetPanel.Name = "alphabetPanel";
        alphabetPanel.Size = new Size(980, 30);
        alphabetPanel.TabIndex = 4;
        // 
        // listView1
        // 
        listView1.Columns.AddRange(new ColumnHeader[] { columnHeader2, columnHeader5, columnHeader8, columnHeader6, columnHeader7 });
        listView1.FullRowSelect = true;
        listView1.Location = new Point(10, 110);
        listView1.Name = "listView1";
        listView1.Size = new Size(980, 420);
        listView1.TabIndex = 1;
        listView1.UseCompatibleStateImageBehavior = false;
        listView1.View = View.Details;
        // 
        // columnHeader2
        // 
        columnHeader2.Text = "名称";
        columnHeader2.Width = 400;
        // 
        // columnHeader5
        // 
        columnHeader5.Text = "添加日期";
        columnHeader5.Width = 100;
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
        // columnHeader7
        // 
        columnHeader7.Text = "删除";
        columnHeader7.Width = 80;
        // 
        // statusStrip1
        // 
        statusStrip1.Items.AddRange(new ToolStripItem[] { ModCountTextBlock });
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
        Controls.Add(searchPanel);
        Controls.Add(alphabetPanel);
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
