using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace MudServer.DAL;

[Table("Item")]
[PrimaryKey(nameof(Id))]
public class Item(string name, string description, string type)
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public DateTime CreatedDate { get; set; }
    public string Name { get; set; } = name;
    public string Description { get; set; } = description;
    [Column("ItemType")]
    public string Type { get; set; } = type;
}
