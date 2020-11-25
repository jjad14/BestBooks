using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace BestBooks.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }
        public string Description { get; set; }

        [Required]
        public string ISBN { get; set; }

        [Required]
        public string Author { get; set; }
        public string ImageUrl { get; set; }

        // The original price of the product
        [Required]
        [Range(1, 10000)]
        public double ListPrice { get; set; }

        // This price is valid if you order quantity is less than 50.
        [Required]
        [Range(1, 10000)]
        public double Price { get; set; }

        // This price is valid if your order quantity is between 50 and 99 
        [Required]
        [Range(1, 10000)]
        public double Price50 { get; set; }

        // This price is valid if your order quantity exceeds 100
        [Required]
        [Range(1, 10000)]
        public double Price100 { get; set; }

        [Required]
        public int CategoryId { get; set; }
        
        [ForeignKey("CategoryId")]
        public Category Category { get; set; }

        [Required]
        public int CoverTypeId { get; set; }

        [ForeignKey("CoverTypeId")]
        public CoverType CoverType { get; set; }
    }
}
