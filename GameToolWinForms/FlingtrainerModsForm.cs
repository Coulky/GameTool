using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace GameToolWinForms;

public partial class FlingtrainerModsForm : Form
{
    private List<FlingtrainerMod> flingtrainerMods;
    private List<ModItem> existingMods;
    private Form1 mainForm;

    public FlingtrainerModsForm(List<FlingtrainerMod> mods, List<ModItem> existing, Form1 form)
    {
        InitializeComponent();
        flingtrainerMods = mods;
        existingMods = existing;
        mainForm = form;
        LoadModsList();
    }

    private void InitializeComponent()
    {
        this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
        this.label1 = new System.Windows.Forms.Label();
        this.txtSearch = new System.Windows.Forms.TextBox();
        this.btnSearch = new System.Windows.Forms.Button();
        this.listView1 = new System.Windows.Forms.ListView();
        this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
        this.columnHeader2 = new System.Windows.Forms.ColumnHeader();
        this.columnHeader3 = new System.Windows.Forms.ColumnHeader();
        this.btnAddSelected = new System.Windows.Forms.Button();
        this.SuspendLayout();
        // 
        // flowLayoutPanel1
        // 
        this.flowLayoutPanel1.Location = new System.Drawing.Point(12, 10);
        this.flowLayoutPanel1.Name = "flowLayoutPanel1";
        this.flowLayoutPanel1.Size = new System.Drawing.Size(453, 30);
        this.flowLayoutPanel1.TabIndex = 0;
        // 
        // label1
        // 
        this.label1.AutoSize = true;
        this.label1.Location = new System.Drawing.Point(12, 50);
        this.label1.Name = "label1";
        this.label1.Size = new System.Drawing.Size(59, 15);
        this.label1.TabIndex = 1;
        this.label1.Text = "搜索关键词";
        // 
        // txtSearch
        // 
        this.txtSearch.Location = new System.Drawing.Point(80, 47);
        this.txtSearch.Name = "txtSearch";
        this.txtSearch.Size = new System.Drawing.Size(300, 23);
        this.txtSearch.TabIndex = 2;
        // 
        // btnSearch
        // 
        this.btnSearch.Location = new System.Drawing.Point(390, 47);
        this.btnSearch.Name = "btnSearch";
        this.btnSearch.Size = new System.Drawing.Size(75, 23);
        this.btnSearch.TabIndex = 3;
        this.btnSearch.Text = "搜索";
        this.btnSearch.UseVisualStyleBackColor = true;
        this.btnSearch.Click += new System.EventHandler(this.btnSearch_Click);
        // 
        // listView1
        // 
        this.listView1.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
        this.columnHeader1,
        this.columnHeader2,
        this.columnHeader3});
        this.listView1.FullRowSelect = true;
        this.listView1.Location = new System.Drawing.Point(12, 80);
        this.listView1.Name = "listView1";
        this.listView1.Size = new System.Drawing.Size(453, 260);
        this.listView1.TabIndex = 4;
        this.listView1.UseCompatibleStateImageBehavior = false;
        this.listView1.View = System.Windows.Forms.View.Details;
        // 
        // columnHeader1
        // 
        this.columnHeader1.Text = "名称";
        this.columnHeader1.Width = 250;
        // 
        // columnHeader2
        // 
        this.columnHeader2.Text = "游戏";
        this.columnHeader2.Width = 150;
        // 
        // columnHeader3
        // 
        this.columnHeader3.Text = "状态";
        this.columnHeader3.Width = 50;
        // 
        // btnAddSelected
        // 
        this.btnAddSelected.Location = new System.Drawing.Point(390, 350);
        this.btnAddSelected.Name = "btnAddSelected";
        this.btnAddSelected.Size = new System.Drawing.Size(75, 23);
        this.btnAddSelected.TabIndex = 5;
        this.btnAddSelected.Text = "添加选中";
        this.btnAddSelected.UseVisualStyleBackColor = true;
        this.btnAddSelected.Click += new System.EventHandler(this.btnAddSelected_Click);
        // 
        // FlingtrainerModsForm
        // 
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(477, 385);
        this.Controls.Add(this.btnAddSelected);
        this.Controls.Add(this.listView1);
        this.Controls.Add(this.btnSearch);
        this.Controls.Add(this.txtSearch);
        this.Controls.Add(this.label1);
        this.Controls.Add(this.flowLayoutPanel1);
        this.Name = "FlingtrainerModsForm";
        this.Text = "修改器库";
        this.Load += new System.EventHandler(this.FlingtrainerModsForm_Load);
        this.ResumeLayout(false);
        this.PerformLayout();

    }

    private Label label1;
    private TextBox txtSearch;
    private Button btnSearch;
    private ListView listView1;
    private ColumnHeader columnHeader1;
    private ColumnHeader columnHeader2;
    private ColumnHeader columnHeader3;
    private Button btnAddSelected;
    private FlowLayoutPanel flowLayoutPanel1;

    private void LoadModsList()
    {
        listView1.Items.Clear();
        foreach (var mod in flingtrainerMods)
        {
            var item = new ListViewItem(mod.Name);
            item.SubItems.Add(mod.Game);
            // 检查是否已存在
            var exists = existingMods.Any(m => m.Url == mod.Url);
            item.SubItems.Add(exists ? "已添加" : "未添加");
            listView1.Items.Add(item);
        }
    }

    private void FlingtrainerModsForm_Load(object sender, EventArgs e)
    {
        // 添加 A 到 Z 的字母按钮
        for (char c = 'A'; c <= 'Z'; c++)
        {
            Button btn = new Button();
            btn.Text = c.ToString();
            btn.Size = new System.Drawing.Size(30, 25);
            btn.Click += new System.EventHandler(this.AlphabetButton_Click);
            flowLayoutPanel1.Controls.Add(btn);
        }
    }

    private void AlphabetButton_Click(object sender, EventArgs e)
    {
        Button btn = sender as Button;
        if (btn != null)
        {
            char letter = btn.Text[0];
            listView1.Items.Clear();
            foreach (var mod in flingtrainerMods)
            {
                if (mod.Name.StartsWith(letter.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    var item = new ListViewItem(mod.Name);
                    item.SubItems.Add(mod.Game);
                    // 检查是否已存在
                    var exists = existingMods.Any(m => m.Url == mod.Url);
                    item.SubItems.Add(exists ? "已添加" : "未添加");
                    listView1.Items.Add(item);
                }
            }
        }
    }

    private void btnSearch_Click(object sender, EventArgs e)
    {
        var searchTerm = txtSearch.Text.ToLower();
        listView1.Items.Clear();
        int count = 0;
        foreach (var mod in flingtrainerMods)
        {
            if (mod.Name.ToLower().Contains(searchTerm) || mod.Game.ToLower().Contains(searchTerm))
            {
                var item = new ListViewItem(mod.Name);
                item.SubItems.Add(mod.Game);
                // 检查是否已存在
                var exists = existingMods.Any(m => m.Url == mod.Url);
                item.SubItems.Add(exists ? "已添加" : "未添加");
                listView1.Items.Add(item);
                count++;
            }
        }
        if (count == 0)
        {
            MessageBox.Show("没有找到匹配的修改器", "搜索结果", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void btnAddSelected_Click(object sender, EventArgs e)
    {
        if (listView1.SelectedItems.Count > 0)
        {
            foreach (ListViewItem item in listView1.SelectedItems)
            {
                var modName = item.SubItems[0].Text;
                var gameName = item.SubItems[1].Text;
                var status = item.SubItems[2].Text;
                if (status != "已添加")
                {
                    var mod = flingtrainerMods.FirstOrDefault(m => m.Name == modName && m.Game == gameName);
                    if (mod != null)
                    {
                        mainForm.AddModFromLibrary(mod);
                        item.SubItems[2].Text = "已添加";
                    }
                }
            }
            MessageBox.Show("已添加选中的修改器", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}