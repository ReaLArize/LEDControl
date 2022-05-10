using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LEDControl.Dtos;

namespace LEDControl.Database.Models;

public class Device
{
    [Key]
    public Guid Id { get; set; }
    
    [MaxLength(128)]
    public string Name { get; set; }
    public DeviceMode Mode { get; set; }
    
    [MaxLength(50)]
    public string Hostname { get; set; }
    public int Port { get; set; }
    
    public int NumLeds { get; set; }
    
    [NotMapped]
    public LightRequest LightRequest { get; set; }
}