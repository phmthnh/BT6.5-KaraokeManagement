using System.Data;
using Microsoft.Data.SqlClient;
using KaraokeManagement.DTO;

namespace KaraokeManagement.DAL;

internal sealed class RoomDAL
{
    internal DataTable Rooms()=>DatabaseHelper.Query("SELECT MaPhong,TenPhong,LoaiPhong,GiaTheoGio,TrangThai FROM PhongHat ORDER BY MaPhong");
    internal DataTable Services()=>DatabaseHelper.Query("SELECT MaDV,TenDV,DonGia,TonKho FROM DichVu ORDER BY MaDV");
    internal DataTable Details(int room)=>DatabaseHelper.Query("""
    SELECT d.MaDV,v.TenDV,d.SoLuong,d.DonGia,d.SoLuong*d.DonGia AS ThanhTien
    FROM HoaDon h JOIN ChiTietDichVu d ON d.MaHD=h.MaHD JOIN DichVu v ON v.MaDV=d.MaDV
    WHERE h.MaPhong=@room AND h.TrangThai=0 ORDER BY d.MaDV
    """,Db.Int("@room",room));
    internal DataTable Report()=>DatabaseHelper.Query("""
    SELECT h.MaHD,p.TenPhong,h.GioVao,h.GioRa,h.TongTienPhong,h.TongTienDichVu,h.TongTienPhong+h.TongTienDichVu AS TongTien
    FROM HoaDon h JOIN PhongHat p ON p.MaPhong=h.MaPhong WHERE h.TrangThai=1 ORDER BY h.MaHD DESC
    """);
    internal DataTable Current(int room)=>DatabaseHelper.Query("SELECT MaHD,GioVao FROM HoaDon WHERE MaPhong=@room AND TrangThai=0",Db.Int("@room",room));

    internal int Open(int room)=>Db.Transaction((c,t)=>
    {
        if(Db.Execute(c,t,"UPDATE PhongHat SET TrangThai=N'Đang hát' WHERE MaPhong=@room AND TrangThai=N'Trống'",Db.Int("@room",room))!=1)
            throw new InvalidOperationException("Phòng không còn trống.");
        return (int)Db.Scalar(c,t,"INSERT HoaDon(MaPhong,GioVao) OUTPUT INSERTED.MaHD VALUES(@room,GETDATE())",Db.Int("@room",room))!;
    });

    private static decimal LockRoom(SqlConnection c,SqlTransaction t,int room)
    {
        if(Db.Scalar(c,t,"SELECT GiaTheoGio FROM PhongHat WITH(UPDLOCK,HOLDLOCK) WHERE MaPhong=@room AND TrangThai=N'Đang hát'",Db.Int("@room",room)) is decimal rate)
            return rate;
        throw new InvalidOperationException("Chọn phòng đang hát.");
    }

    internal void Add(int room,int service,int qty)=>Db.Transaction((c,t)=>
    {
        LockRoom(c,t,room);
        var id=(int)Db.Scalar(c,t,"SELECT MaHD FROM HoaDon WHERE MaPhong=@room AND TrangThai=0",Db.Int("@room",room))!;
        var price=Db.Scalar(c,t,"SELECT DonGia FROM DichVu WHERE MaDV=@service",Db.Int("@service",service));
        if(price is not decimal unit) throw new InvalidOperationException("Dịch vụ không còn tồn tại.");
        Db.Execute(c,t,"""
        IF EXISTS(SELECT 1 FROM ChiTietDichVu WHERE MaHD=@id AND MaDV=@service)
            UPDATE ChiTietDichVu SET SoLuong=SoLuong+@qty WHERE MaHD=@id AND MaDV=@service;
        ELSE INSERT ChiTietDichVu VALUES(@id,@service,@qty,@price);
        """,Db.Int("@id",id),Db.Int("@service",service),Db.Int("@qty",qty),Db.Money("@price",unit));
        return 0;
    });

    internal RoomBill Pay(int room,Func<DateTime,DateTime,decimal,decimal> calculate)=>Db.Transaction((c,t)=>
    {
        var rate=LockRoom(c,t,room);
        int id;
        DateTime from,to;
        using(var command=Db.Command(c,t,"SELECT MaHD,GioVao,GETDATE() FROM HoaDon WHERE MaPhong=@room AND TrangThai=0",Db.Int("@room",room)))
        using(var reader=command.ExecuteReader())
        {
            if(!reader.Read()) throw new InvalidOperationException("Phòng không có hóa đơn đang mở.");
            id=reader.GetInt32(0);from=reader.GetDateTime(1);to=reader.GetDateTime(2);
        }
        var items=new List<(int Service,int Quantity,decimal Price)>();
        using(var command=Db.Command(c,t,"SELECT MaDV,SoLuong,DonGia FROM ChiTietDichVu WHERE MaHD=@id ORDER BY MaDV",Db.Int("@id",id)))
        using(var reader=command.ExecuteReader())
            while(reader.Read()) items.Add((reader.GetInt32(0),reader.GetInt32(1),reader.GetDecimal(2)));
        var bill=new RoomBill(id,from,to,calculate(from,to,rate),items.Sum(i=>i.Quantity*i.Price));
        if(bill.TongTienPhong>9999999999999999.99m || bill.TongTienDichVu>9999999999999999.99m) throw new ArgumentException("Tổng tiền vượt giới hạn.");
        Db.Execute(c,t,"UPDATE HoaDon SET GioRa=@end,TongTienPhong=@roomMoney,TongTienDichVu=@serviceMoney,TrangThai=1 WHERE MaHD=@id",
            Db.Date("@end",to),Db.Money("@roomMoney",bill.TongTienPhong),Db.Money("@serviceMoney",bill.TongTienDichVu),Db.Int("@id",id));
        foreach(var item in items)
            if(Db.Execute(c,t,"UPDATE DichVu SET TonKho=TonKho-@qty WHERE MaDV=@service AND TonKho>=@qty",
                Db.Int("@qty",item.Quantity),Db.Int("@service",item.Service))!=1)
                throw new InvalidOperationException("Không đủ tồn kho dịch vụ. Thanh toán đã hoàn tác.");
        Db.Execute(c,t,"UPDATE PhongHat SET TrangThai=N'Trống' WHERE MaPhong=@room",Db.Int("@room",room));
        return bill;
    });
}
