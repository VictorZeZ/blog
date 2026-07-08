using blog.Domain.Categories.Types;
using blog.Domain.Common;
using blog.Domain.Common.Helpers;

namespace blog.Domain.Categories.Entities
{
    public class Category : SoftDeletableEntity<CategoryId>
    {
        public string Name { get; private set; }
        public string Slug { get; private set; }

        private Category() : base(CategoryId.Empty) { }

        public Category(string name) : base(CategoryId.New())
        {
            Name = name;
            Slug = SlugGenerator.Generate(name);
        }

        public void Update(string name)
        {
            Name = name;
            Slug = SlugGenerator.Generate(name);
            MarkAsUpdated();
        }
    }
}
