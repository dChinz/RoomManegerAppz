using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using RoomManegerApp.Check_in;
using RoomManegerApp.Forms;

namespace RoomManegerApp.Contracts
{
    public partial class FormAvailable_room : Form
    {
        private Action _callback;

        public FormAvailable_room(Action callback)
        {
            InitializeComponent();
            _callback = callback;

            dateTimePicker2.Value = DateTime.Now.AddDays(1);
        }

        private void FormAvailable_room_Load(object sender, EventArgs e)
        {
            //load_status_room();
            UpdateStatusRoom();
        }

        private void loadButton(string sql, Dictionary<string, object> paragramers = null)
        {
            ClearRoomButtons();

            var data = Database_connect.ExecuteReader(sql, paragramers);
            foreach (var row in data)
            {
                string roomName = row["name"].ToString();
                string status = row["status"].ToString();
                int type = Convert.ToInt16(row["type"].ToString());
                int size = Convert.ToInt16(row["size"].ToString());
                string checkin = dateTimePicker1.Value.ToString("dd-MM-yyyy");
                string checkout = dateTimePicker2.Value.ToString("dd-MM-yyyy");

                Button button = new Button();
                button.Text = roomName;
                button.Width = 70;
                button.Height = 40;
                button.Margin = new Padding(5);
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderSize = 1;
                button.Font = new Font("Segoe UI", 10, FontStyle.Bold);
                //if (status == "Trống")
                //{
                    if (size == 0)
                    {
                        button.BackColor = Color.LightGreen;
                        button.ForeColor = Color.DarkGreen; // chữ xanh đậm
                        button.FlatAppearance.BorderColor = Color.SeaGreen; // viền xanh đậm

                        // Hover sáng lên nhẹ
                        button.MouseEnter += (s, e) => button.BackColor = Color.MediumSeaGreen;
                        button.MouseLeave += (s, e) => button.BackColor = Color.LightGreen;
                    }
                    else
                    {
                        button.BackColor = Color.LightBlue;
                        button.ForeColor = Color.DarkBlue;
                        button.FlatAppearance.BorderColor = Color.SeaGreen;

                        // Hover sáng lên nhẹ
                        button.MouseEnter += (s, e) => button.BackColor = Color.DeepSkyBlue;
                        button.MouseLeave += (s, e) => button.BackColor = Color.LightBlue;
                    }

                button.Click += (s, e) =>
                {
                    DialogResult result = MessageBox.Show("Bạn muốn chọn phòng: " + roomName + "?", "Thông báo", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result == DialogResult.Yes)
                    {
                        //FormAdd_check_in f = new FormAdd_check_in(roomName, type, _callback);
                        FormAvailable_Guest f = new FormAvailable_Guest(roomName, type, size, _callback, checkin, checkout);
                        f.ShowDialog();
                    }
                    else if (result == DialogResult.No)
                    {
                        return;
                    }
                    else
                    {
                        return;
                    }
                };
                if (type == 0)
                {
                    flowLayoutPanel1.Controls.Add(button);
                }
                else if (type == 1)
                {
                    flowLayoutPanel2.Controls.Add(button);
                }
                else if (type == 2)
                {
                    flowLayoutPanel3.Controls.Add(button);
                }
                else if (type == 3)
                {
                    flowLayoutPanel4.Controls.Add(button);
                }
                else
                {
                    flowLayoutPanel5.Controls.Add(button);
                }
            }
        }

        private void UpdateStatusRoom()
        {
            string sql = @"select count(status) as total 
                    from rooms
                    where status = @status and type = @type";
            var roomType = new[] { 0, 1, 2, 3, 4 };
            var statuses = new[] { 0, 1, 2 };

            var labelMap = new Dictionary<(int type, int status), Label>
            {
                {(0, 0), label16 },
                {(0, 1), label18 },
                {(0, 2), label20 },

                {(1, 0), label23 },
                {(1, 1), label25 },
                {(1, 2), label27 },

                {(2, 0), label29 },
                {(2, 1), label31 },
                {(2, 2), label33 },

                {(3, 0), label35 },
                {(3, 1), label37 },
                {(3, 2), label39 },

                {(4, 0), label41 },
                {(4, 1), label43 },
                {(4, 2), label45 },
            };

            foreach (var type in roomType)
            {
                foreach (var status in statuses)
                {
                    var key = (type, status);
                    if (labelMap.TryGetValue(key, out var label))
                    {
                        var parameters = new Dictionary<string, object>
                        {
                            {"@status", status },
                            {"@type", type }
                        };
                        label.Text = Database_connect.ExecuteScalar(sql, parameters).ToString();
                    }
                }
            }
        }

        private void dateTimePicker2_ValueChanged(object sender, EventArgs e)
        {
            if(dateTimePicker1.Value >= dateTimePicker2.Value)
            {
                ClearRoomButtons();
                return;
            }
                

            string checkin = dateTimePicker1.Value.ToString("yyyyMMdd");
            string checkout = dateTimePicker2.Value.ToString("yyyyMMdd");

            
            string sql = @"SELECT name, status, type, size
                        FROM rooms
                        WHERE id NOT IN (
                            SELECT room_id
                            FROM checkins
                            WHERE NOT (
                                end_date <= @checkin OR start_date >= @checkout
                            )
                        )";
            Dictionary<string, object> paragrames = new Dictionary<string, object>();
            paragrames.Add("@checkin", checkin);
            paragrames.Add("@checkout", checkout);
            loadButton(sql, paragrames);
        }

        private void dateTimePicker1_ValueChanged(object sender, EventArgs e)
        {
            if (dateTimePicker1.Value >= dateTimePicker2.Value)
            {
                ClearRoomButtons();
                return;
            }

            string checkin = dateTimePicker1.Value.ToString("yyyyMMdd");
            string checkout = dateTimePicker2.Value.ToString("yyyyMMdd");

            string sql = @"SELECT name, status, type, size
                        FROM rooms
                        WHERE id NOT IN (
                            SELECT room_id
                            FROM checkins
                            WHERE NOT (
                                end_date <= @checkin OR start_date >= @checkout
                            )
                        )";
            Dictionary<string, object> paragrames = new Dictionary<string, object>();
            paragrames.Add("@checkin", checkin);
            paragrames.Add("@checkout", checkout);
            loadButton(sql, paragrames);
        }

        private void ClearRoomButtons()
        {
            flowLayoutPanel1.Controls.Clear();
            flowLayoutPanel2.Controls.Clear();
            flowLayoutPanel3.Controls.Clear();
            flowLayoutPanel4.Controls.Clear();
            flowLayoutPanel5.Controls.Clear();
        }

    }
}
