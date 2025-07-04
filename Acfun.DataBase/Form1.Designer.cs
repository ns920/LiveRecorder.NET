namespace Acfun.DataBase
{
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
            listView_Data = new ListView();
            textBox_Search = new TextBox();
            button_Search = new Button();
            button_GetUrl = new Button();
            SuspendLayout();
            // 
            // listView_Data
            // 
            listView_Data.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            listView_Data.Location = new Point(34, 135);
            listView_Data.Name = "listView_Data";
            listView_Data.Size = new Size(1077, 444);
            listView_Data.TabIndex = 0;
            listView_Data.UseCompatibleStateImageBehavior = false;
            // 
            // textBox_Search
            // 
            textBox_Search.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            textBox_Search.Location = new Point(34, 43);
            textBox_Search.Name = "textBox_Search";
            textBox_Search.Size = new Size(1077, 23);
            textBox_Search.TabIndex = 1;
            // 
            // button_Search
            // 
            button_Search.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            button_Search.Location = new Point(1132, 26);
            button_Search.Name = "button_Search";
            button_Search.Size = new Size(123, 57);
            button_Search.TabIndex = 2;
            button_Search.Text = "查询";
            button_Search.UseVisualStyleBackColor = true;
            button_Search.Click += button_Search_Click;
            // 
            // button_GetUrl
            // 
            button_GetUrl.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            button_GetUrl.Location = new Point(1132, 115);
            button_GetUrl.Name = "button_GetUrl";
            button_GetUrl.Size = new Size(123, 57);
            button_GetUrl.TabIndex = 3;
            button_GetUrl.Text = "获取链接";
            button_GetUrl.UseVisualStyleBackColor = true;
            button_GetUrl.Click += button_GetUrl_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1304, 601);
            Controls.Add(button_GetUrl);
            Controls.Add(button_Search);
            Controls.Add(textBox_Search);
            Controls.Add(listView_Data);
            Name = "Form1";
            Text = "Form1";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ListView listView_Data;
        private TextBox textBox_Search;
        private Button button_Search;
        private Button button_GetUrl;
    }
}
