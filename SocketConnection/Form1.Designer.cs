namespace SocketConnection
{
    partial class Form1
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
            this.txtDataBuffer = new System.Windows.Forms.RichTextBox();
            this.btnWriteDigitalData = new System.Windows.Forms.Button();
            this.btnWriteSeriallData = new System.Windows.Forms.Button();
            this.btnClear = new System.Windows.Forms.Button();
            this.btnStop = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // txtDataBuffer
            // 
            this.txtDataBuffer.Location = new System.Drawing.Point(13, 13);
            this.txtDataBuffer.Name = "txtDataBuffer";
            this.txtDataBuffer.Size = new System.Drawing.Size(1138, 560);
            this.txtDataBuffer.TabIndex = 0;
            this.txtDataBuffer.Text = "";
            // 
            // btnWriteDigitalData
            // 
            this.btnWriteDigitalData.Location = new System.Drawing.Point(13, 579);
            this.btnWriteDigitalData.Name = "btnWriteDigitalData";
            this.btnWriteDigitalData.Size = new System.Drawing.Size(125, 23);
            this.btnWriteDigitalData.TabIndex = 1;
            this.btnWriteDigitalData.Text = "Write Digital Data";
            this.btnWriteDigitalData.UseVisualStyleBackColor = true;
            this.btnWriteDigitalData.Click += new System.EventHandler(this.btnWriteDigitalData_Click);
            // 
            // btnWriteSeriallData
            // 
            this.btnWriteSeriallData.Location = new System.Drawing.Point(139, 579);
            this.btnWriteSeriallData.Name = "btnWriteSeriallData";
            this.btnWriteSeriallData.Size = new System.Drawing.Size(152, 23);
            this.btnWriteSeriallData.TabIndex = 2;
            this.btnWriteSeriallData.Text = "Write Serial Data";
            this.btnWriteSeriallData.UseVisualStyleBackColor = true;
            this.btnWriteSeriallData.Click += new System.EventHandler(this.btnWriteSeriallData_Click);
            // 
            // btnClear
            // 
            this.btnClear.Location = new System.Drawing.Point(291, 579);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(152, 23);
            this.btnClear.TabIndex = 3;
            this.btnClear.Text = "Clear";
            this.btnClear.UseVisualStyleBackColor = true;
            this.btnClear.Click += new System.EventHandler(this.btnClear_Click);
            // 
            // btnStop
            // 
            this.btnStop.Location = new System.Drawing.Point(444, 579);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(152, 23);
            this.btnStop.TabIndex = 4;
            this.btnStop.Text = "Stop";
            this.btnStop.UseVisualStyleBackColor = true;
            this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1163, 614);
            this.Controls.Add(this.btnStop);
            this.Controls.Add(this.btnClear);
            this.Controls.Add(this.btnWriteSeriallData);
            this.Controls.Add(this.btnWriteDigitalData);
            this.Controls.Add(this.txtDataBuffer);
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.RichTextBox txtDataBuffer;
        private System.Windows.Forms.Button btnWriteDigitalData;
        private System.Windows.Forms.Button btnWriteSeriallData;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.Button btnStop;
    }
}

