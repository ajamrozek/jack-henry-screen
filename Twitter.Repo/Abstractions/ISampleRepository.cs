using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Twitter.Repo.Models;

namespace Twitter.Repo.Abstractions
{
    public interface ISampleRepository
    {
        Task GetSampleStreamAsync(CancellationToken cancellationToken);
    }
}
