using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RoomManegerApp.Users
{
    public partial class FormAddUser : Form
    {
        public FormAddUser()
        {
            InitializeComponent();
        }

        private void FormAddUser_Load(object sender, EventArgs e)
        {
            
        }

        private void buttonTao_Click(object sender, EventArgs e)
        {
            string username = textBoxUsername.Text.Trim();
            string password = textBoxPassword.Text.Trim();
            string fullname = textBoxFullname.Text.Trim();
            string STRrole = comboBoxRole.Text;
            int role = 0;
            if (STRrole == "Admin") role = 0;
            else if (STRrole == "Manager") role = 1;
            else if (STRrole == "Staff") role = 2;

            try
            {
                string sql = @"insert into users (username, password, role, fullname) values (@username, @password, @role, @fullname)";
                int rowAffected = Convert.ToInt32(Database_connect.ExecuteNonQuery(sql, new Dictionary<string, object>
                {
                    {"@username", username },
                    { "@password", password },
                    {"@role", role },
                    { "@fullname", fullname}
                }));
                if (rowAffected > 0)
                {
                    MessageBox.Show("Thêm người dùng thành công", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi đã xảy ra " + ex.Message,"Thông báo", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
            }
        }
    }
}
