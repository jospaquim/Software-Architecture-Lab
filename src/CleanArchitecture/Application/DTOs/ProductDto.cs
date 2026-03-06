namespace CleanArchitecture.Application.DTOs;

public class ProductDto
{
    public Guid Uid { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public bool IsActive { get; set; }
    public bool IsInStock { get; set; }
    public string? ImageUrl { get; set; }
    public Guid CategoryUid { get; set; }
    public string CategoryName { get; set; } = string.Empty;
}

public class ProductDetailsDto : ProductDto
{
    public CategoryDto Category { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateProductDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public Guid CategoryUid { get; set; }
    public string? ImageUrl { get; set; }
}

public class UpdateProductDto
{
    public Guid Uid { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public Guid CategoryUid { get; set; }
    public string? ImageUrl { get; set; }
}

public class CategoryDto
{
    public Guid Uid { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int ProductCount { get; set; }
}

public class CreateCategoryDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
