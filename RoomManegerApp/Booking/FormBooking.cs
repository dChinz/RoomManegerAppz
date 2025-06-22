﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace RoomManegerApp.Booking
{
    public partial class FormBooking : Form
    {
        public FormBooking()
        {
            InitializeComponent();

            AddOptionButton();
        }

        private async void FormBooking_Load(object sender, EventArgs e)
        {
            await load_form();
        }
        private async Task load_form()
        {
            dataGridView1.Rows.Clear();
            string sql = "select id, name, phone, email, roomSize, checkin, checkout, type from booking";
            var data = await Task.Run(() => Database_connect.ExecuteReader(sql));
            foreach (var row in data)
            {
                FillDataGridView(row);
            }
        }

        private void FillDataGridView(Dictionary<string, object> row)
        {
            DateTime.TryParseExact(row["checkin"].ToString(), "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime dbcheckin);
            string checkin = dbcheckin.ToString("dd/MM/yyyy");

            DateTime.TryParseExact(row["checkout"].ToString(), "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime dbcheckout);
            string checkout = dbcheckout.ToString("dd/MM/yyyy");

            dataGridView1.Rows.Add(row["id"], row["name"], row["phone"], row["email"], row["roomSize"], checkin, checkout, row["type"]);
        }

        private void AddOptionButton()
        {
            DataGridViewButtonColumn btnAccept = new DataGridViewButtonColumn();
            btnAccept.HeaderText = "";
            btnAccept.Name = "accept";
            btnAccept.Text = "Xác nhận";
            btnAccept.UseColumnTextForButtonValue = true;
            dataGridView1.Columns.Add(btnAccept);

            DataGridViewButtonColumn btnCancel = new DataGridViewButtonColumn();
            btnCancel.HeaderText = "";
            btnCancel.Name = "cancel";
            btnCancel.Text = "Hủy";
            btnCancel.UseColumnTextForButtonValue = true;
            dataGridView1.Columns.Add(btnCancel);
        }

        private async void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            DataGridViewRow row = dataGridView1.Rows[e.RowIndex];
            string id = row.Cells["id"].Value.ToString();
            string name = row.Cells["name"].Value.ToString();
            string phone = row.Cells["phone"].Value.ToString();
            string email = row.Cells["email"].Value.ToString();
            string roomSize = row.Cells["roomSize"].Value.ToString();
            string checkin = row.Cells["checkin"].Value.ToString();
            string checkout = row.Cells["checkout"].Value.ToString();
            string type = row.Cells["type"].Value.ToString();

            if (e.ColumnIndex == 8)
            {
                try
                {
                    //Kiểm tra và cập nhật khách hàng
                    string sql = "select 1 from tenants where name = @name";
                    int rowCount = Convert.ToInt32(Database_connect.ExecuteScalar(sql, new Dictionary<string, object> { { "@name", name } }));
                    if (rowCount > 0)
                    {
                        sql = @"update tenants set phone = @phone, email = @email where name = @name";
                    }
                    else
                    {
                        sql = @"insert into tenants (name, phone, email) values (@name, @phone, @email)";
                    }
                    Database_connect.ExecuteNonQuery(sql, new Dictionary<string, object>
                    {
                        { "@name", name},
                        { "@email", email },
                        { "@phone", phone},
                    });

                    //Lấy phòng tương ứng
                    sql = @"select id
                                from rooms
                                where status = @status and size = @size and type = @type
                                and id NOT IN (
                                SELECT room_id
                                FROM checkins
                                WHERE NOT (
                                    end_date <= @checkin OR start_date >= @checkout
                                        )
                                    )
                                order by id asc
                                limit 1";
                    int idRoom = Convert.ToInt32(Database_connect.ExecuteScalar(sql, new Dictionary<string, object>
                    {
                        { "@status", "Trống"},
                        { "@type", type},
                        { "@size", roomSize },
                    }));
                    if (idRoom == 0)
                    {
                        MessageBox.Show("Không có phòng tương ứng.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    //Lấy giá phòng
                    sql = @"select price from rooms where id = @id";
                    double price = Convert.ToDouble(Database_connect.ExecuteScalar(sql, new Dictionary<string, object> { { "@id", idRoom } }));

                    //Lấy id khách hàng
                    sql = @"select id from tenants where name = @name";
                    int idTenant = Convert.ToInt32(Database_connect.ExecuteScalar(sql, new Dictionary<string, object> { { "@name", name } }));
                    if (idTenant == 0)
                    {
                        MessageBox.Show("Không có khách hàng tương ứng.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    DateTime.TryParseExact(checkin, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime checkinDate);
                    DateTime.TryParseExact(checkout, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime checkoutDate);

                    double total = (checkoutDate - checkinDate).Days * price;

                    //Tạo checkins 
                    sql = @"insert into checkins (room_id, tenant_id, start_date, end_date, deposit) values (@room_id, @tenant_id, @start_date, @end_date, @deposit)";
                    int rowAffected = Convert.ToInt32(Database_connect.ExecutiveInsertAndGetId(sql, new Dictionary<string, object>
                    {
                        { "@room_id", idRoom},
                        { "@tenant_id", idTenant},
                        { "@start_date", checkinDate.ToString("yyyyMMdd")},
                        { "@end_date", checkoutDate.ToString("yyyyMMdd")},
                        { "@deposit", total},
                    }));



                    sql = @"insert into bills (checkins_id, userId, status, create_date) values (@checkins_id, @userId, @status, @create_date)";
                    Database_connect.ExecuteNonQuery(sql, new Dictionary<string, object>
                    {
                        { "@checkins_id", rowAffected},
                        { "@userId", Session.Role},
                        { "@status", "Thanh toán toàn bộ"},
                        { "@create_date", DateTime.Now.ToString("yyyy-MM-dd")}
                    });
                    if (rowAffected > 0)
                    {

                        sql = @"delete from booking where id = @id";
                        Database_connect.ExecuteNonQuery(sql, new Dictionary<string, object> { { "@id", id } });

                        MessageBox.Show("Xác nhận thành công", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        await load_form();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi đã xảy ra: {ex}");
                }
            }
            else if(e.ColumnIndex == 9)
            {
                DialogResult result = MessageBox.Show("Xác nhận hủy?", "Thông báo", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                if (result == DialogResult.Yes)
                {
                    string sql = @"delete from booking where id = @id";
                    Database_connect.ExecuteNonQuery(sql, new Dictionary<string, object> { { "@id", id } });

                    MessageBox.Show("Hủy thành công", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    await load_form();
                }
                else
                {
                    return;
                }
                
            }
            if (e.RowIndex < 0 || e.RowIndex >= dataGridView1.Rows.Count)
                return;
        }
    }
}
