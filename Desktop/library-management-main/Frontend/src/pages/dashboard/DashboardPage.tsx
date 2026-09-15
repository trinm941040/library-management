import { useState } from 'react'
import {
  ArrowUpRight,
  BookOpen,
  Check,
  ChevronDown,
  CircleAlert,
  Clock,
  Plus,
  RefreshCw,
  Search,
  Users,
  X,
} from 'lucide-react'
import { useAuth } from '../../auth/AuthProvider'
import { Button } from '@/common/components/ui/button'
import { Input } from '@/common/components/ui/input'
import { MetricCard } from './components/MetricCard'

const queue = [
  {
    member: 'Olivia Martin',
    item: 'The Midnight Library',
    date: 'Hết hạn hôm nay',
    tone: 'amber',
    initials: 'OM',
  },
  {
    member: 'Noah Williams',
    item: 'Tomorrow, and Tomorrow, and Tomorrow',
    date: 'Quá hạn 2 ngày',
    tone: 'red',
    initials: 'NW',
  },
  {
    member: 'Amelia Brown',
    item: 'The Creative Act',
    date: 'Quá hạn 3 ngày',
    tone: 'red',
    initials: 'AB',
  },
  {
    member: 'Ethan Davis',
    item: 'A Brief History of Time',
    date: 'Hết hạn ngày mai',
    tone: 'slate',
    initials: 'ED',
  },
]

export function DashboardPage() {
  const { user } = useAuth()
  const [search, setSearch] = useState('')
  const [notice, setNotice] = useState('')
  const filteredQueue = queue.filter((record) =>
    `${record.member} ${record.item}`.toLowerCase().includes(search.toLowerCase()),
  )
  const displayName = user?.displayName ?? 'Người dùng'

  const runCheckout = () => {
    setNotice('Đã hoàn tất mượn sách cho Olivia Martin.')
    window.setTimeout(() => setNotice(''), 4000)
  }

  return (
    <div className="content-wrap">
      <div className="welcome-row">
        <div>
          <p className="eyebrow">THỨ NĂM, NGÀY 21 THÁNG 8 NĂM 2026</p>
          <h1>
            Chào buổi sáng, {displayName} <span>✦</span>
          </h1>
          <p className="subheading">Tổng quan hoạt động thư viện hôm nay.</p>
        </div>
        <Button className="primary-button" type="button" onClick={runCheckout}>
          <Plus aria-hidden="true" /> Tạo phiếu mượn
        </Button>
      </div>
      {notice && (
        <div className="toast" role="status">
          <Check aria-hidden="true" />
          {notice}
          <Button
            variant="ghost"
            size="icon"
            type="button"
            aria-label="Đóng thông báo"
            onClick={() => setNotice('')}
          >
            <X />
          </Button>
        </div>
      )}
      <section className="metrics" aria-label="Chỉ số tổng quan thư viện">
        <MetricCard
          label="Sách đang được mượn"
          value="1,284"
          delta="12.4%"
          positive
          icon={<BookOpen />}
          tone="blue"
        />
        <MetricCard
          label="Độc giả đang hoạt động"
          value="2,847"
          delta="4.8%"
          positive
          icon={<Users />}
          tone="green"
        />
        <MetricCard
          label="Sách quá hạn"
          value="36"
          delta="8.2%"
          icon={<CircleAlert />}
          tone="orange"
        />
        <MetricCard
          label="Sách phải trả hôm nay"
          value="58"
          delta="2.1%"
          positive
          icon={<Clock />}
          tone="violet"
        />
      </section>
      <div className="section-heading">
        <div>
          <h2>Trung tâm xử lý</h2>
          <p>Các công việc ưu tiên cần bạn chú ý.</p>
        </div>
        <Button variant="link" className="text-button" type="button">
          Xem tất cả hoạt động <ArrowUpRight aria-hidden="true" />
        </Button>
      </div>
      <section className="action-grid">
        <article className="focus-card">
          <div className="card-top">
            <span className="card-icon purple">
              <RefreshCw />
            </span>
            <span className="tag">LƯU THÔNG</span>
          </div>
          <h3>Duy trì luân chuyển sách.</h3>
          <p>Có 8 phiếu đặt sẵn sàng nhận và 12 lượt trả đang chờ xử lý.</p>
          <Button variant="link" className="card-link" type="button">
            Mở quầy lưu thông <ArrowUpRight />
          </Button>
          <div className="card-arc" />
        </article>
        <article className="queue-card">
          <div className="queue-header">
            <div>
              <h3>Sắp đến hạn và quá hạn</h3>
              <p>Theo dõi sớm để hạn chế phát sinh vi phạm.</p>
            </div>
            <span className="queue-total">Tổng 36</span>
          </div>
          <div className="queue-search">
            <Search aria-hidden="true" />
            <Input
              aria-label="Tìm bản ghi sắp đến hạn và quá hạn"
              placeholder="Tìm độc giả hoặc tên sách..."
              value={search}
              onChange={(event) => setSearch(event.target.value)}
            />
          </div>
          <div className="queue-list">
            {filteredQueue.map((record) => (
              <div className="queue-row" key={record.member}>
                <span className="avatar avatar-blue">{record.initials}</span>
                <span className="record-info">
                  <b>{record.member}</b>
                  <small>{record.item}</small>
                </span>
                <span className={`due-label ${record.tone}`}>{record.date}</span>
                <Button
                  variant="ghost"
                  size="icon"
                  className="row-more"
                  aria-label={`Mở hồ sơ của ${record.member}`}
                  type="button"
                >
                  •••
                </Button>
              </div>
            ))}
            {filteredQueue.length === 0 && (
              <p className="empty-state">Không có bản ghi lưu thông phù hợp.</p>
            )}
          </div>
        </article>
      </section>
      <div className="section-heading lower">
        <div>
          <h2>Hoạt động gần đây</h2>
          <p>Các cập nhật mới nhất từ nhóm của bạn.</p>
        </div>
        <Button variant="outline" className="filter-button" type="button">
          7 ngày qua <ChevronDown />
        </Button>
      </div>
      <section className="activity-table">
        <div className="table-row table-head">
          <span>HOẠT ĐỘNG</span>
          <span>NHÂN VIÊN</span>
          <span>THỜI GIAN</span>
          <span>TRẠNG THÁI</span>
        </div>
        <div className="table-row">
          <span className="activity-name">
            <span className="mini-icon green">
              <Check />
            </span>
            <b>Đã xử lý trả sách</b>
            <small>The House in the Cerulean Sea</small>
          </span>
          <span>Alex Morgan</span>
          <span>10 phút trước</span>
          <span className="status complete">Hoàn tất</span>
        </div>
        <div className="table-row">
          <span className="activity-name">
            <span className="mini-icon blue">
              <Users />
            </span>
            <b>Đã thêm độc giả mới</b>
            <small>Mã thành viên #MB-2849</small>
          </span>
          <span>Jamie Davis</span>
          <span>42 phút trước</span>
          <span className="status complete">Hoàn tất</span>
        </div>
      </section>
    </div>
  )
}
