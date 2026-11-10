using System.Data;
using Microsoft.Data.SqlClient;
using KaraokeManagement.BUS;
using KaraokeManagement.DTO;

namespace KaraokeManagement.GUI;

public partial class MainForm : Form
{
    public MainForm()
    {
        InitializeComponent();
        Shown+=(_,_)=>Run(LoadScreen);
    }
    private void Run(Action action)
    {
        try { action(); }
        catch(Exception ex) when(ex is SqlException or ArgumentException or InvalidOperationException or OverflowException or IOException or UnauthorizedAccessException)
        {
            var message=ex is SqlException sql ? sql.Number==547 ? "Dữ liệu đang được sử dụng hoặc mã tham chiếu không tồn tại." : "Không thể thao tác với SQL Server. Kiểm tra kết nối và dữ liệu." : ex.Message;
            MessageBox.Show(this,message,"Thông báo",MessageBoxButtons.OK,MessageBoxIcon.Warning);
        }
    }
    private void Catalog(object? sender,EventArgs args)=>Run(()=>
    {
        using var dialog=new CatalogForm();
        dialog.ShowDialog(this);
        LoadScreen();
    });


    private readonly RoomBUS service=new();
    private int selectedRoom;

    private void LoadScreen()
    {
        rooms.SuspendLayout();
        try
        {
            foreach(Control old in rooms.Controls.Cast<Control>().ToArray()) old.Dispose();
            foreach(DataRow row in service.Rooms().Rows)
            {
                var id=(int)row["MaPhong"];
                var empty=(string)row["TrangThai"]=="Trống";
                var button=new Button
                {
                    Text=$"{row["TenPhong"]}\n{row["TrangThai"]}",
                    Size=new Size(148,78),
                    BackColor=empty?Color.SeaGreen:Color.Firebrick,
                    ForeColor=Color.White,
                    FlatStyle=FlatStyle.Flat
                };
                button.Click+=(_,_)=>Run(()=>SelectRoom(id));
                rooms.Controls.Add(button);
            }
        }
        finally { rooms.ResumeLayout(); }
        services.DisplayMember="TenDV";
        services.ValueMember="MaDV";
        services.DataSource=service.Services();
        report.DataSource=service.Report();
        SelectRoom(selectedRoom);
    }
    private void SelectRoom(int id)
    {
        selectedRoom=id;
        var room=service.Rooms().AsEnumerable().SingleOrDefault(row=>row.Field<int>("MaPhong")==id);
        var current=service.Current(id);
        var active=current.Rows.Count>0;
        roomInfo.Text=room is null?"Chọn phòng hát.":$"{room["TenPhong"]} | {room["TrangThai"]} | {room["GiaTheoGio"]:N2} đ/giờ";
        startTime.Text=active?$"Giờ vào: {current.Rows[0]["GioVao"]:dd/MM/yyyy HH:mm:ss}":"";
        openButton.Enabled=room is not null && !active;
        addButton.Enabled=payButton.Enabled=active;
        details.DataSource=service.Details(id);
    }
    private void OpenRoom(object? sender,EventArgs args)=>Run(()=>{service.Open(selectedRoom);LoadScreen();});
    private void AddService(object? sender,EventArgs args)=>Run(()=>
    {
        if(services.SelectedValue is not int id) throw new ArgumentException("Chọn dịch vụ.");
        service.Add(selectedRoom,id,(int)quantity.Value);
        SelectRoom(selectedRoom);
    });
    private void PayRoom(object? sender,EventArgs args)=>Run(()=>
    {
        if(MessageBox.Show(this,"Xác nhận thanh toán phòng?","Thanh toán",MessageBoxButtons.YesNo)!=DialogResult.Yes) return;
        var bill=service.Pay(selectedRoom);
        LoadScreen();
        MessageBox.Show(this,$"Hóa đơn {bill.MaHD}\nThời gian hát: {bill.GioRa-bill.GioVao:hh\\:mm\\:ss}\nTiền phòng: {bill.TongTienPhong:N2} đ\nTiền dịch vụ: {bill.TongTienDichVu:N2} đ\nTổng tiền: {bill.TongTien:N2} đ","Thanh toán thành công");
    });

}
