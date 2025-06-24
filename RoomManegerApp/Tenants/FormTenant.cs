﻿using System;
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
using RoomManegerApp.Tetants;

namespace RoomManegerApp.Forms
{
    public partial class FormTenant : Form
    {
        public FormTenant()
        {
            InitializeComponent();

            // Bật DoubleBuffering để giảm giật khi cuộn
            typeof(DataGridView).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.SetProperty,
                null, dataGridView1, new object[] { true });

            dataGridView1.ReadOnly = true;
            dataGridView1.RowHeadersVisible = false;

            this.AcceptButton = buttonSearch;

            SetPlaceholderText(textBoxName, "Nhập tên...");
            SetPlaceholderText(textBoxId_card, "Nhập số CCCD...");

            dataGridView1.AllowUserToResizeRows = false;
            dataGridView1.AllowUserToResizeColumns = false;
        }

        private int currentPage = 1;
        private int pageSize = 21;
        private int totalRecords = 0;
        private int totalPages = 0;

        public async void reloadData()
        {
            await load_tentant();
        }

        private async void FormTenant_Load(object sender, EventArgs e)
        {
            await load_tentant();
        }

        private async Task load_tentant()
        {
            UpdatePaginationInfo();
            positionIndex();
            try
            {
                int offset = (currentPage - 1) * pageSize;
                string sql = @"select * from tenants limit @pageSize offset @offset";
                var data = await Task.Run(() => Database_connect.ExecuteReader(sql, new Dictionary<string, object>
                {
                    { "@pageSize", pageSize },
                    { "@offset", offset}
                }));

                dataGridView1.Invoke(new Action(() =>
                {
                    dataGridView1.Rows.Clear();
                    foreach (var row in data)
                    {
                        int DBgender = Convert.ToInt16(row["gender"].ToString());
                        string gender = "";
                        if (DBgender == 0) gender = "Nam";
                        else if (DBgender == 1) gender = "Nữ";

                        dataGridView1.Rows.Add(row["id"], row["name"], row["phone"], row["email"], row["id_card"], gender, row["address"]);
                    }

                    if (scrollPosition >= 0 && scrollPosition < dataGridView1.Rows.Count)
                    {
                        dataGridView1.FirstDisplayedScrollingRowIndex = scrollPosition;
                    }
                }));
                labelPageInfo.Text = $"Trang {currentPage}/{totalPages}";

            }
            catch (Exception ex)
            {
                MessageBox.Show("lỗi khi tải dữ liệu: " +  ex.Message, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdatePaginationInfo()
        {
            string sql = "SELECT COUNT(*) FROM tenants";
            totalRecords = Convert.ToInt32(Database_connect.ExecuteScalar(sql));
            totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
        }

        private int scrollPosition = 0;
        private void positionIndex()
        {
            if(dataGridView1.FirstDisplayedScrollingRowIndex > 0)
            {
                scrollPosition = dataGridView1.FirstDisplayedScrollingRowIndex;
            }
        }

        private void buttonAdd_one_tentant_Click(object sender, EventArgs e)
        {
            FormAdd_one_tenant f = new FormAdd_one_tenant();
            f.ShowDialog();

            f.tentant_added += async (s, args) => await load_tentant();
        }

        private void dataGridView1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var hit = dataGridView1.HitTest(e.X, e.Y);
                if (hit.RowIndex >= 0)
                {
                    // Chọn dòng chuột phải vào
                    dataGridView1.ClearSelection();
                    dataGridView1.Rows[hit.RowIndex].Selected = true;

                    // Hiển thị menu tại vị trí chuột
                    contextMenuStrip1.Show(dataGridView1, e.Location);
                }
            }
        }

        public int get_id_room()
        {
            if(dataGridView1.SelectedRows.Count > 0 &&
                int.TryParse(dataGridView1.SelectedRows[0].Cells[0].Value?.ToString(), out int id))
            {
                return id;
            }
            return -1;
        }

        private void sửaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int id = get_id_room();
            if (id < 0) return;

            var f = new FormAdd_one_tenant(id, reloadData);
            f.Text = "Cập nhật thông tin khách hàng";
            positionIndex();
            f.ShowDialog();

           
        }

        private async void xóaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string sql = @"delete from tenants where id = @id";
            int id = get_id_room();
            int rowAffected = Database_connect.ExecuteNonQuery(sql, new Dictionary<string, object> { { "@id", id } });
            if (rowAffected > 0) 
            {
                MessageBox.Show("Xoá thành công", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                positionIndex();
                await load_tentant();
            }
            else
            {
                MessageBox.Show("Xoá không thành công hoặc không tìm thấy bản ghi.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        private void SetPlaceholderText(TextBox textBox, string placeholder)
        {
            // Nếu TextBox rỗng, đặt giá trị là placeholder và thay đổi màu chữ thành Gray
            if (string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = placeholder;
                textBox.ForeColor = Color.Gray;
            }

            // Khi người dùng click vào TextBox (focus vào)
            textBox.Enter += (s, e) =>
            {
                if (textBox.Text == placeholder)
                {
                    textBox.Text = "";
                    textBox.ForeColor = Color.Black;
                }
            };

            // Khi người dùng rời khỏi TextBox (mất focus)
            textBox.Leave += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(textBox.Text))
                {
                    textBox.Text = placeholder;
                    textBox.ForeColor = Color.Gray;
                }
            };
        }

        private void buttonSearch_Click(object sender, EventArgs e)
        {
            currentPage = 1;
            load_search();
        }

        private async void load_search()
        {
            string name = textBoxName.Text;
            string id_card = textBoxId_card.Text;

            var conditions = new List<string>();
            var parameters = new Dictionary<string, object>();

            if (!string.IsNullOrEmpty(name) && name != "Nhập tên...")
            {
                conditions.Add("name like '%' || @name || '%'");
                parameters.Add("@name", name);
            }
            if (!string.IsNullOrEmpty(id_card) && id_card != "Nhập số CCCD...")
            {
                if (Int64.TryParse(id_card, out long tmp))
                {
                    conditions.Add("id_card = @id_card");
                    parameters.Add("@id_card", id_card);
                }
                else
                {
                    MessageBox.Show("Số CCCD không hợp lệ.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            if(conditions.Count == 0)
            {
                await load_tentant();
                return;
            }

            string sql = "select * from tenants";
            if (conditions.Count > 0)
            {
                sql += " where " + string.Join(" and ", conditions);
            }
            sql += " limit @pageSize offset @offset";
            parameters.Add("@pageSize", pageSize);
            parameters.Add("@offset", (currentPage - 1) * pageSize);
            try
            {
                var data = Database_connect.ExecuteReader(sql, parameters);
                dataGridView1.Rows.Clear();

                foreach (var row in data)
                {
                    dataGridView1.Rows.Add(row["id"], row["name"], row["phone"], row["email"], row["id_card"], row["gender"], row["address"]);
                }

                sql = "select count(*) from tenants";
                if (conditions.Count > 0)
                {
                    sql += " where " + string.Join(" and ", conditions);
                }
                totalRecords = Convert.ToInt32(Database_connect.ExecuteScalar(sql, parameters));
                totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
                labelPageInfo.Text = $"Trang {currentPage}/{totalPages}";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi tìm kiếm: " + ex.Message, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void btnFirst_Click(object sender, EventArgs e)
        {
            currentPage = 1;
            await load_tentant();
        }

        private async void btnPrev_Click(object sender, EventArgs e)
        {
            if(currentPage > 1)
            {
                currentPage--;
                await load_tentant();
            }
        }

        private async void btnNext_Click(object sender, EventArgs e)
        {
            if(currentPage < totalPages)
            {
                currentPage++;
                await load_tentant();
            }
        }

        private async void btnLast_Click(object sender, EventArgs e)
        {
            currentPage = totalPages;
            await load_tentant();
        }
    }
}
