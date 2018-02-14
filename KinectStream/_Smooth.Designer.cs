using System.Windows.Forms;

namespace SmoothStream
{
    partial class SmoothOperator
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.imageStream = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.imageStream)).BeginInit();
            this.SuspendLayout();
            // 
            // imageStream
            // 
            this.imageStream.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.imageStream.Location = new System.Drawing.Point(0, 0);
            this.imageStream.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.imageStream.Name = "imageStream";
            this.imageStream.Size = new System.Drawing.Size(1920, 1080);
            this.imageStream.TabIndex = 3;
            this.imageStream.TabStop = false;
            // 
            // SmoothOperator
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(1922, 1083);
            this.Controls.Add(this.imageStream);
            this.Font = new System.Drawing.Font("Courier New", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ForeColor = System.Drawing.Color.White;
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SmoothOperator";
            this.ShowIcon = false;
            this.Text = "SmoothOperator";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.SmoothStream_FormClosing);
            this.Load += new System.EventHandler(this.SmoothOperator_Load);
            ((System.ComponentModel.ISupportInitialize)(this.imageStream)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.PictureBox imageStream;

        public PictureBox ImageStream
        {
            get
            {
                return imageStream;
            }

            set
            {
                imageStream = value;
            }
        }
    }
}

