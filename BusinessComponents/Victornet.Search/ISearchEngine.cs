using Lucene.Net.Documents;
using Lucene.Net.Search;
using System;
using System.Collections.Generic;
namespace Victornet.Search
{
	public interface ISearchEngine
	{
		void RebuildIndex(IEnumerable<Document> indexDocuments, bool isBeginning, bool isEndding);
		void Insert(Document indexDocument, bool reopen = true);
		void Insert(IEnumerable<Document> indexDocuments, bool reopen = true);
		void Delete(string id, string fieldNameOfId, bool reopen = true);
		void Delete(IEnumerable<string> ids, string fieldNameOfId, bool reopen = true);
		void Update(Document indexDocument, string id, string fieldNameOfId, bool reopen = true);
		void Update(IEnumerable<Document> indexDocuments, IEnumerable<string> ids, string fieldNameOfId, bool reopen = true);
		PagingDataSet<Document> Search(Query searchQuery, Filter filter, Sort sort, int pageIndex, int pageSize);
		IEnumerable<Document> Search(Query searchQuery, Filter filter, Sort sort, int topNumber);
		double GetIndexSize();
		DateTime GetLastModified();
	}
}
