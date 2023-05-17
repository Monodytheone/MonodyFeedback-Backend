namespace FAQService.Domain.Entities;

public class Page
{
    public Guid Id { get; init; }

    public Tab Tab { get; init; }

    public int Sequence { get; private set; }

    public bool IsHot { get; private set; }

    public string Title { get; private set; }

    /// <summary>
    /// 是否是纯Q&A
    /// <para>为false时为html</para>
    /// </summary>
    public bool IsPureQandA { get; private set; }

    public string? HtmlUrl { get; private set; }

    public List<QandA> QandAs { get; private set; } = new();

    
    public Page ChangeSequence(int value)
    {
        Sequence = value;
        return this;
    }

    public Page ChangeTitle(string value)
    {
        Title = value;
        return this;
    }

    public Page ChangeIsHot(bool value)
    {
        IsHot = value;
        return this;
    }

    public Page ToPureQandA()
    {
        IsPureQandA = true;
        HtmlUrl = null;
        return this;
    }

    public Page ToHtml(string htmlUrl)
    {
        IsPureQandA = false;
        QandAs.Clear();
        HtmlUrl = htmlUrl;
        return this;
    }

    // 简单一点，把sequence的获取放到领域服务里吧
    public Page AddQandA(int sequenceInPage, string answer, string question)
    {
        if (IsPureQandA == false)
        {
            throw new FieldAccessException("内容为外部html的Page不得直接添加Q&A");
        }
        QandAs.Add(new QandA(sequenceInPage, question, answer));
        return this;
    }

    public Page SortQandAs(Guid[] sortedIds)
    {
        if (IsPureQandA == false)
        {
            throw new InvalidOperationException("内容为外部Html的Page不可对Q&A进行排序");
        }

        IEnumerable<Guid> qandAIdsInDb = QandAs.Select(qandA => qandA.Id);
        if (qandAIdsInDb.SequenceIgnoreEqual(sortedIds) == false)
        {
            throw new Exception("待排序Id必须是所有的Q&AId");
        }

        int seq = 1;
        foreach (Guid id in sortedIds)
        {
            QandA? qandA = QandAs.FirstOrDefault(qandA => qandA.Id == id);
            if (qandA == null)
            {
                throw new NullReferenceException($"Q&AId = {id} 不存在");
            }
            qandA.ChangeSequence(seq);
            seq++;  // 没有把++和赋值写在一起的必要，除了平添阅读障碍以外没什么用
        }
        return this;
    }

    public Page RemoveQandA(Guid qandAId)
    {
        QandAs = QandAs.OrderBy(qandA => qandA.Sequence).ToList();
        int indexOfRemove = QandAs.FindIndex(qandA => qandA.Id == qandAId);
        QandAs.RemoveAt(indexOfRemove);  // 移除
        for (int i = indexOfRemove; i < QandAs.Count; i++)  // 更新Sequence
        {
            QandAs[i].ChangeSequence(QandAs[i].Sequence - 1);
        }
        return this;
    }

    private Page() { }

    public class Builder
    {
        private int _sequence;
        private bool _isHot;
        private string _title;
        private bool? _isPureQandA;
        private string? _htmlUrl;

        public Builder PureQandA(int sequence, bool isHot, string title)
        {
            _isPureQandA = true;
            _sequence = sequence;
            _title = title;
            _isHot = isHot;
            return this;
        }

        public Builder Html(int sequence, bool isHot, string title, string htmlUrl)
        {
            _isPureQandA = false;
            _sequence = sequence;
            _isHot= isHot;
            _title = title;
            _htmlUrl = htmlUrl;
            return this;
        }

        public Page Build()
        {
            if (_isPureQandA == null)
            {
                throw new ArgumentNullException();
            }
            if (_isPureQandA == false && _htmlUrl == null)
            {
                throw new ArgumentOutOfRangeException("当Page内容为外部html时，必须指定htmlUrl");
            }
            if (_isPureQandA == true && _htmlUrl != null)
            {
                throw new ArgumentOutOfRangeException("当Page内容为纯Q&A时，htmlUrl必须为空");
            }
            Page page = new()
            {
                Sequence = _sequence,
                IsHot = _isHot,
                Title = _title,
                IsPureQandA = (bool)_isPureQandA,
                HtmlUrl = _htmlUrl,
            };
            return page;
        }
    }
}
