namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Dtos.Category;

public class CategoryEditDto
{
    public int Id { get; set; }
    public string CategoryName { get; set; }
    public int CategorySortOrder { get; set; }
    public bool IsActive { get; set; }
}
