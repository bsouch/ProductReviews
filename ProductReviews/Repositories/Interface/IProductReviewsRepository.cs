﻿using ProductReviews.DomainModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProductReviews.Repositories.Interface
{
    public interface IProductReviewsRepository
    {
        public Task<List<ProductReviewModel>> GetAllProductReviewsAsync();
        public Task<List<ProductReviewModel>> GetAllVisibleProductReviewsForProductAsync(int ID);
        public Task<ProductReviewModel> GetProductReviewAsync(int ID);
        public ProductReviewModel CreateProductReview(ProductReviewModel productReviewModel);
        public void UpdateProductReview(ProductReviewModel productReviewModel);
        public Task SaveChangesAsync();
    }
}
