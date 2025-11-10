using ETICARET.DataAccess.Abstract;
using ETICARET.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETICARET.DataAccess.Concrete
{
    public class EfCoreCommentDal : EfCoreGenericRepository<Comment,DataContext>, ICommentDal
    {
    }
}
