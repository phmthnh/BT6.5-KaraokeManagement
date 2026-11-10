using System.Data;
using KaraokeManagement.DAL;
using KaraokeManagement.DTO;

namespace KaraokeManagement.BUS;

public sealed class RoomBUS
{
    private readonly RoomDAL dal=new();
    public DataTable Rooms()=>dal.Rooms();
    public DataTable Services()=>dal.Services();
    public DataTable Current(int room)=>dal.Current(room);
    public DataTable Details(int room)=>dal.Details(room);
    public DataTable Report()=>dal.Report();
    public int Open(int room)=>dal.Open(room);
    public void Add(int room,int service,int quantity)
    {
        if(room<=0 || service<=0 || quantity<=0) throw new ArgumentException("Chọn phòng, dịch vụ và số lượng hợp lệ.");
        dal.Add(room,service,quantity);
    }
    public RoomBill Pay(int room)=>dal.Pay(room,CalculateRoom);
    public static decimal CalculateRoom(DateTime start,DateTime end,decimal rate)
    {
        if(end<start) throw new InvalidOperationException("Giờ ra phải từ giờ vào.");
        return Math.Round((end-start).Ticks/(decimal)TimeSpan.TicksPerHour*rate,2,MidpointRounding.AwayFromZero);
    }
}
