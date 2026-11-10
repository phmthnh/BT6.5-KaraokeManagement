namespace KaraokeManagement.DTO;

public sealed record RoomBill(int MaHD,DateTime GioVao,DateTime GioRa,decimal TongTienPhong,decimal TongTienDichVu)
{
    public decimal TongTien=>TongTienPhong+TongTienDichVu;
}
