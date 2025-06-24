using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace RoomManegerApp.Romms
{
    public partial class FormAdd_one_room : Form
    {
        private Action _callback;
        public FormAdd_one_room(Action callback)
        {
            InitializeComponent();

            this.StartPosition = FormStartPosition.CenterScreen;
            _callback = callback;
        }
        private int id;
        public FormAdd_one_room(int id, Action callback)
        {
            InitializeComponent();

            this.StartPosition = FormStartPosition.CenterScreen;
            this.id = id;
            _callback = callback;
        }

        private void FormAdd_room_Load(object sender, EventArgs e)
        {
            load_add_room();
        }

        private void load_add_room()
        {
            if (id != 0)
            {
                textBox2.ReadOnly = true;
                buttonCapnhat.Text = "Cập nhật";

                string sql = @"select * from rooms where id = @id";
                var data = Database_connect.ExecuteReader(sql, new Dictionary<string, object> { { "@id", id } });
                foreach (var row in data)
                {
                    int DBtype = Convert.ToInt16(row["type"].ToString());
                    string type = "";
                    if (DBtype == 0) type = "Standard";
                    else if (DBtype == 1) type = "Superior";
                    else if (DBtype == 2) type = "Deluxe";
                    else if (DBtype == 3) type = "Executive";
                    else if (DBtype == 4) type = "VIP";

                    int DBsize = Convert.ToInt16(row["size"].ToString());
                    string size = "";
                    if (DBsize == 0) size = "Đơn";
                    else if (DBsize == 1) size = "Đôi";

                    textBox1.Text = row["name"].ToString();
                    comboBox1.Text = type;
                    comboBoxSize.Text = size;
                    textBox4.Text = row["note"].ToString();
                }
            }
            else
            {
                textBox2.ReadOnly = true;
                textBox1.Text = null;
                textBox4.Text = null;
                comboBox1.SelectedIndex = 0;
                comboBoxSize.SelectedIndex = 0;

                string sql = @"select name, type, price, size from rooms order by id desc limit 1";
                var data = Database_connect.ExecuteReader(sql);
                foreach (var row in data)
                {
                    int DBtype = Convert.ToInt16(row["type"].ToString());
                    string type = "";
                    if (DBtype == 0) type = "Standard";
                    else if (DBtype == 1) type = "Superior";
                    else if (DBtype == 2) type = "Deluxe";
                    else if (DBtype == 3) type = "Executive";
                    else if (DBtype == 4) type = "VIP";

                    int DBsize = Convert.ToInt16(row["size"].ToString());
                    string size = "";
                    if (DBsize == 0) size = "Đơn";
                    else if (DBsize == 1) size = "Đôi";

                    textBox2.Text = row["name"].ToString() + ", " + type + ", " + row["price"].ToString() + ", " + size;
                }
            }
        }

        private void buttonCapnhat_Click(object sender, EventArgs e)
        {
            string name = textBox1.Text.Trim();
            string status = label7.Text.Trim();
            string STRtype = comboBox1.Text.Trim();
            string STRsize = comboBoxSize.Text.Trim();
            double price = 0;
            string note = textBox4.Text.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Vui lòng nhập tên phòng!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (string.IsNullOrWhiteSpace(STRtype))
            {
                MessageBox.Show("Vui lòng chọn loại phòng!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            int type = 0;

            if(STRtype == "Standard")
            {
                type = 0;
                price = 1300000;
            }
            else if(STRtype == "Superior")
            {
                type = 1;
                price = 1500000;
            }
            else if (STRtype == "Deluxe")
            {
                type = 2;
                price = 1800000;
            }
            else if (STRtype == "Executive")
            {
                type = 3;
                price = 2000000;
            }
            else if (STRtype == "VIP")
            {
                type = 4;
                price = 2500000;
            }


            int size = 0;
            if(STRsize == "Đôi")
            {
                size = 1;
                price += 100000;
            }

            if(id != 0)
            {
                string sql = @"update rooms set name = @name, type = @type, price = @price, size = @size, note = @note where id = @id";
                Database_connect.ExecuteNonQuery(sql, new Dictionary<string, object>
                {
                    { "@id", id},
                    { "@name", name},
                    { "@type", type},
                    { "@price", price},
                    { "@size", size},
                    { "@note", note}
                });
                MessageBox.Show("Cập nhật thông tin phòng thành công.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                // Gọi event nếu có người đăng ký
                room_added?.Invoke(this, EventArgs.Empty);
                this.Close();
            }
            else
            {
                string sql = @"select 1 from rooms where name = @name";
                int row = Convert.ToInt16(Database_connect.ExecuteScalar(sql, new Dictionary<string, object> { { "@name", name } }));
                if (row == 0)
                {
                    sql = @"insert into rooms (name, status, type, size, price, note) values(@name, @status, @type, @size, @price, @note)";
                    int rowAffected = Convert.ToInt16(Database_connect.ExecuteNonQuery(sql, new Dictionary<string, object>
                    {
                        { "@name", name},
                        { "@status", status },
                        { "@type", type },
                        { "@size", size },
                        { "@price", price },
                        { "@note", note },
                    }));
                    if (rowAffected > 0)
                    {
                        MessageBox.Show("Thêm thành công", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                       
                    }
                    else
                    {
                        MessageBox.Show("Thêm thất bại", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Phòng đã tồn tại.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            // Gọi event nếu có người đăng ký
            //room_added?.Invoke(this, EventArgs.Empty);
            this.Close();

            _callback?.Invoke();

        }

        public event EventHandler room_added;
    }
}
