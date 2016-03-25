using Lucene.Net.Index;
using Lucene.Net.Search;
using System;
namespace Victornet.Search
{
	public class SearcherManager
	{
		private IndexSearcher searcher;
		private IndexWriter writer;
		private object thisLock = new object();
		public SearcherManager(IndexWriter writer)
		{
			this.writer = writer;
			this.searcher = new IndexSearcher(writer.GetReader());
		}
		public void Reopen()
		{
			lock (this.thisLock)
			{
				IndexSearcher indexSearcher = this.searcher;
				try
				{
					indexSearcher.GetIndexReader().IncRef();
					IndexReader indexReader = indexSearcher.GetIndexReader();
					if (!indexReader.IsCurrent())
					{
						IndexReader indexReader2 = indexReader.Reopen();
						if (indexReader2 != indexReader)
						{
							this.searcher.GetIndexReader().DecRef();
							this.searcher = new IndexSearcher(indexReader2);
						}
					}
				}
				finally
				{
					indexSearcher.GetIndexReader().DecRef();
				}
			}
		}
		public IndexSearcher GetSearcher()
		{
			IndexSearcher result;
			lock (this.thisLock)
			{
				this.searcher.GetIndexReader().IncRef();
				result = this.searcher;
			}
			return result;
		}
		public void ReleaseSearcher()
		{
			this.ReleaseSearcher(this.searcher);
		}
		public void ReleaseSearcher(IndexSearcher searcher)
		{
			lock (this.thisLock)
			{
				searcher.GetIndexReader().DecRef();
			}
		}
		public void Close()
		{
			this.ReleaseSearcher();
			this.searcher = null;
		}
	}
}
