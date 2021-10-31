﻿using Microsoft.EntityFrameworkCore;
using ProductReviews.Context;
using ProductReviews.DomainModels;
using ProductReviews.Repositories.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProductReviews.Repositories.Concrete
{
    public class SqlProductReviewsRepository : IProductReviewsRepository
    {
        private readonly Context.Context _context;
        public SqlProductReviewsRepository(Context.Context context)
        {
            _context = context;
        }

        public Task<IEnumerable<ProductReviewModel>> GetAllProductReviewsAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<ProductReviewModel> GetProductReviewAsync(int ID)
        {
            return await _context._productReviews.FirstOrDefaultAsync(d => d.ProductReviewID == ID);
        }

        public void CreateProductReviewAsync(ProductReviewModel productReviewModel)
        {
            _context._productReviews.Add(productReviewModel);
        }

        public void UpdateProductReview(ProductReviewModel productReviewModel)
        {
            //EF tracks the changes of updates. It pushes them to the DB when SaveChangesAsync() has been called.
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}