using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Linq;
using System.Security.Authentication.ExtendedProtection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RoomManegerApp
{
    public partial class FormDangNhap : Form
    {
        public FormDangNhap()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterScreen;
        }

        private void FormDangNhap_Load(object sender, EventArgs e)
        {
            textBoxPassword.UseSystemPasswordChar = true;
            this.AcceptButton = button1;

            label3.Visible = false;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            textBoxPassword.UseSystemPasswordChar = !checkBox1.Checked;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string sql = @"select id, username, role, fullname from users where username = @username and password = @password";
            string username = textBoxUsername.Text.Trim();
            string password = textBoxPassword.Text.Trim();
            var data = Database_connect.ExecuteReader(sql, new Dictionary<string, object>
            {
                {"@username", username},
                {"@password", password}
            });
            if (data.Count == 0)
            {
                label3.Visible = true;
                label3.Text = "Tài khoản hoặc mật khẩu không đúng";
                return;
            }
            foreach (var row in data)
            {
                int DBrole = Convert.ToInt16(row["role"].ToString());
                string role = "";
                if (DBrole == 0) role = "Admin";
                else if (DBrole == 1) role = "Manager";
                else if (DBrole == 2) role = "Staff";

                Session.UserId = Convert.ToInt32(row["id"]);
                Session.UserName = row["username"].ToString();
                Session.Role = role;
                Session.Fullname = row["fullname"].ToString();

                Forms.FormDashboard f = new Forms.FormDashboard();
                f.WindowState = FormWindowState.Maximized;
                this.Hide();
                f.ShowDialog();
                this.Close();
            }
        }
    }
}
