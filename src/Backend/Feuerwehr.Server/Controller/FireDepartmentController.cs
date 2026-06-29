using Feuerwehr.Common.Models.Dto;
using Microsoft.AspNetCore.Mvc;

namespace Feuerwehr.Server.Controller;

[Route("api/[controller]")]
[ApiController]
public class FireDepartmentController : ControllerBase
{
    IEnumerable<FireDepartmentDto> _fireDepartmentDtos;
    
    public FireDepartmentController()
    {
        _fireDepartmentDtos = new List<FireDepartmentDto>()
        {
            new FireDepartmentDto()
            {
                Id = 1,
                Name = "Feuerwehr Musterstadt",
                District = "Stadt Musterstadt",
                Municipality = "Ost Holstein",
                State = "Schleswig Holstein"
            }
        };
            
    }
    
    [HttpGet]
    public IEnumerable<FireDepartmentDto> Get()
    {
        return _fireDepartmentDtos;
    }

    [HttpPost]
    public void Add([FromBody] FireDepartmentDto fireDepartmentDto)
    {
        _fireDepartmentDtos.ToList().Add(fireDepartmentDto);
    }

    [HttpGet("{id}")]
    public FireDepartmentDto Get(int id)
    {
        return _fireDepartmentDtos.FirstOrDefault(x => x.Id == id);
    }
    
}