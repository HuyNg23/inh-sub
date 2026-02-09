## 2.3.7 (2025-12-22)
### Features
- None
### Bug Fix
- Fix lỗi xử lý step Download và forward file sang 3rd

## 2.3.6 (2025-12-08)
### Features
- None
### Bug Fix
- Fix lỗi xử lý step exception ở step runCallAPI
- Fix lỗi xử lý step exception ở step runCallXAPI
- Fix lỗi lưu log exception bằng result api, trong màn hình lịch sử api 3rd ở step runCallXAPI
- Fix lỗi xử lý định dạng ảnh .png và .jpg ở step runCallXAPI

## 2.3.5 (2025-12-03)
### Features
- Bổ sung nhận file base64 đẩy sang 3rd ở step Call Rest XAPI
### Bug Fix
- Fix double backslash khi BindingData

## 2.3.4 (2025-11-25)
### Features
- Chuyển step đẩy file sang 3rd thành step Call Rest XAPI
- Bổ sung step Download và forward file sang 3rd
### Bug Fix
- None

## 2.3.3 (2025-10-06)
### Features
- Xem header khi gọi api trong màn hình lịch sử workflow
- Thêm step đẩy file sang 3rd trong cấu hình workflow
- Thêm step step run script trong cấu hình workflow
- Thay đổi ghi log file theo tenant
- Bug Fix
### Bug Fix
None

## 2.3.2 (2025-05-21)
### Features
None
### Bug Fix
- Fix lỗi định dạng chuỗi và danh sách của step trong wf
- Fix tìm kiếm lịch sử wf, api, sql 

## 2.3.1 (2025-05-21)
### Features
- Thêm tính năng lưu lịch sử api dùng sqlite
- Thêm tính năng lưu lịch sử workflow dùng sqlite
- Thêm tính năng lưu lịch sử querrySQL dùng sqlite
### Bug Fix
- Fix triển khai workflow trên cùng 1 server
- Fix data dữ liệu dạng int, float và long cho phép truyền dữ liệu null

## 2.2.5 (2024-12-30)
### Features
- Thêm tính năng cấu hình layout
- Thêm tính năng cấu hình trang
- Thêm tính năng tự động đánh index các trường trong lịch sử Schedule/Workflow/ApiThirdParty khi tạo mới tenant
### Bug Fix
- Fix Close và Open BOT FPT khi end chat tự động trên IBox
- Fix đọc file đoạn chat 5s 1 lần và chạy 5 luồng riêng biệt 
- Fix đọc file thông tin KH 10s 1 lần và chạy 5 luồng riêng biệt
- Fix chạy CallBack đối với API CreateInteraction CRM sau khi CRM trả kết quả tự cập nhật vào db chat
- Fix chạy định danh thông tin KH khi có kết quả cập nhật thông tin vào DB ChatBot
- Fix tìm kiếm các thông tin trong bảng của lịch sử phiên chat và lịch sử khách hàng
- Fix tìm kiếm lịch sử Schedule/Workflow/ApiThirdParty
- Fix lỗi kết nối hệ thống ORACLE
- Fix lỗi cho phép bổ sung lựa chọn kết nối tới hệ cơ sở dữ liệu SQL server và ORACLE

## 2.2.4 (2024-10-25)
### Features
- None
### Bug Fix
- Sửa lỗi định danh KH linking IC và CRM
- Sửa lỗi chat tin nhắn từ KH và Agent realtime
- Hiển thị ICID, Tên KH của ChatBot, CustomerId của IC
- Sửa lỗi hiển thị tin nhắn của Agent
- Định danh tối ưu chat session và customer để định danh
- Tối ưu đẩy dữ liệu chat sang IC
- Sửa lỗi cập nhật interaction crm vào chatbot khi chuyển giao thời gian sang 1 phút
- Sửa lỗi không gặp tvv không update định danh contact id và interaction id
- Sửa lỗi đẩy tin nhắn miss lúc được lúc không
- Sửa lỗi data đẩy từ IC vào IBox khi create session đầy đủ thông tin
- Sửa lỗi tạo phiên chat khi không có interaction 
- Sửa lỗi gán contact id, thêm số phone vào interaction khi KH không gặp tvv
- Thay đổi thời gian đọc file tin nhắn 1 phút 1 lần và thời gian đọc file customer định danh 2 phút 1 lần tránh trường hợp customer định danh vào thời điểm chưa tạo interaction
- Fix lỗi Agent chat với KH sau đó hoàn thành phiên chat luôn Ibox tạo thêm 1 interaction
- Fix lỗi đẩy full tin nhắn khi gặp tvv (lỗi xảy ra khi các tin nhắn chưa insert kịp vào db đã gặp tvv bị miss tin nhắn đó)
- Fix thêm phần khi CRM đẩy interaction sẽ chạy wf cập nhật luồng contactId và Interaction Id vào CRM, IC bỏ đk check phone và check contactId có value mới đẩy vào
- Fix hiển thị tin nhắn KH yêu cầu gặp tư vấn viên 2 lần trên IC
- Fix thêm api get customer để thực hiện lấy contactId phục vụ Tag
- Fix đẩy tin nhắn 15s 1 lần và định danh 30s 1 lần
- Fix show tin nhắn KH, Agent, Bot 
- Fix lưu tin nhắn thiếu từ Agent đẩy vào khi đã hoàn tất
- Fix đẩy tin nhắn sang IC tránh dup tin nhắn khi Agent cần can thiệp
- Fix định danh để gán tag trên IC khi luồng định danh vào sau
- Fix end chat tự động lấy theo tin nhắn cuối cùng
- Fix end Chat kiểm tra data ngày hôm qua và data trước nó để thực hiện đóng phiên chat
## 2.2.3 (2024-09-09)
### Features
- None
### Bug Fix
- Sửa lỗi lấy dữ liệu luồng định danh khi BOT gửi thông tin vào ChatBot 

## 2.2.2 (2024-09-04)
### Features
- None
### Bug Fix
- Load Config ChatBot trên 2 service ChatBot và service Log triển khai ngay lập tức
- Sửa lỗi GetAll/Create/Update trên RequestGadgetTool các field Name, Url, Body bị null khi Create/Update
- Restore workflow trùng tên với các Id khác trong workflow/nhóm obj/obj/step
- Mã hóa mật khẩu kết nối với ChatBot
- Sửa lỗi thêm 2 field (Ev_Tag, WF_UpdateIC_Interaction)config Chat Bot thực hiện linking IC và CRM

## 2.2.1 (2024-08-26)
### Features
- Thêm service ChatBot tích hợp hệ thống FPTBot với IC (nhận và gửi tin nhắn qua lại giữa BOT - KH và IC (agent))
- Thêm tính năng định danh KH trên CRM và IC khi Bot gửi thông tin
- Thêm tính năng tạo phiên chat trên CRM đối với tin nhắn đầu tiên được đẩy vào từ BOT
- Thêm tính năng thực hiện bất đồng bộ khi call API bên thứ 3/Thực hiện câu lệnh SQL/Thực thi câu lệnh Informix
- Thêm tính năng xem lịch sử phiên chat
- Thêm tính năng cấu hình ChatBot
- Thêm tính năng thực thi câu lệnh Informix trên Workflow
- Thêm tính năng triển khai Workflow trên Schedule/ChatBot
- Thêm tính năng thu hồi Workflow trên Schedule/ChatBot trên Workflow
- Thêm tính năng cấu hình timeout khi gọi API bên thứ 3
- Mở rộng tính năng xem log service Store/ChatBot/Log
- Mở rộng tính năng trên service Log thực hiện lưu log tin nhắn và đẩy tin nhắn vào IC khi agent cần can thiệp hoặc khi KH yêu cầu gặp tư vấn viên
- Mở rộng tính năng hiển thị lịch sử tin nhắn cho Agent để agent xem log và can thiệp khi cần
- Mở rộng tính năng kết nối đến hệ quản trị cơ sở dữ liệu Informix
- Bổ sung dashboard hiển thị báo cáo API bên thứ 3/ChatBot/Schedule/Workflow
- Bổ sung tính năng tự động xóa lịch sử schedule/API bên thứ 3/workflow và giữ lại 3 tháng gần nhất
- Tối ưu lại thư viện schedule
### Bug Fix
- Sửa lỗi schedule không chạy Workflow sau khi được update
- Sửa lỗi chạy job ngay bị nhảy sang site khác
- Sửa lỗi call API nhiều lần trong Thẻ kế hoạch ở Schedule
- Sửa lỗi step khi cấu hình param trong workflow không nhận null ở step trước
- Sửa lỗi không call được API khi call API bằng Request Tool
- Sửa lỗi khởi tạo sql dependency khi triển khai IBox cho một khách hàng mới

## 2.1.4 (2024-05-27)
### Features
- Thêm mới tính năng mail alert: Cảnh báo bộ phận vận hành trong trường hợp có thay đổi bất thường trên Ibox bao gồm
    + Không call được API tích hợp
    + API expose từ IBOX không thể tìm thấy
    + Không kết nối được database
    + Hệ thống đang bị truy cập từ tài khoản tenant [ABC]
    + API được triển khai
    + API được thu hồi
    + API bị khóa
    + Tenant bị xóa
    + Khôi phục tenant
    + Thay đổi cấu hình dịch vụ IBox
    + Lỗi bất thường xảy ra.
- Thêm tính năng shrink log đối với hệ thống SQL alwayson / stand
- Thêm chức năng hỗ trợ format dữ liệu dạng chuỗi.
- Thêm service hỗ trợ backup/restore workflow. Mã hóa tệp tin được backup và chỉ đọc được bởi hệ thống Ibox.
- Mở rộng tính năng monitoring dành riêng cho service schedule, đảm bảo tính sẵn sàng của hệ thống trong trường hợp phát sinh rủi ro, hỗ trợ rủi ro liên quan đến việc network bị down hoặc bị delay. Sử dụng giải pháp listening broker.
- Mở rộng tính năng kết nối đến hệ quản trị cơ sở dữ liệu My SQL, Postgre SQL
- Mở rộng tính năng lọc trong báo cáo quá trình theo dõi kết nối API.
- Bổ sung thêm điều kiện lọc trên báo cáo log lỗi tích hợp API bên thứ 3

### Bug Fix
- Fix lỗi gửi request API trên màn hình gadget tool box của tenant.
- Fix lỗi tạo tài khoản có dạng email thay vì tài khoản thông thường

## 2.1.3 (2024-03-29)
### Features
- Chuyển chức năng gửi mail từ đồng bộ sang bất đồng bộ

### Bug Fix
- Fix lỗi tạo trường dữ liệu cho Object được phép trùng với 2 object khác nhau
- Fix lỗi call API từ chức năng Request tool IBox khi chọn call tại Server IBox
- Tạm ngưng sử dụng dịch vụ ghi log.
- Fix lối đóng connection đã mở để tránh mất tài nguyên
- Cải thiện hiệu năng của chức năng ghi log
- Cải thiện cấu trúc source code, refactoring source code
- Loại bỏ chức năng giải mã, mã hóa của Vietinbank khỏi giao diện.
- Fix lỗi start schedule immediately
- Fix lỗi không sử dụng được duyệt phần tử bất đồng bộ

## 2.1.2 (2024-03-27)
### Features
none

### Bug Fix
- Fix lỗi scan code sonar. Loại bỏ các warning về format dữ liệu, cấu trúc object, kiểm tra điều kiện null
- Fix lỗi scan code sonar. Clear code, giảm thiểu độ phức tạp trong logic

## 2.1.1 (2024-03-25)
### Features
- Cho phép cấu hình triển khai các node và đồng bộ config tại các node đã triển khai của IBox.
- Xây dựng trang quản lý service tại các node triển khai IBox
- Cho phép tải file log trên service schedule
- Cập nhật tính năng xác nhận tenant bị xóa, bắt buộc quản trị viên phải nhập mật khẩu
- Cho phép khôi phục tenant bị xóa.

### Bug Fix
- Fix lỗi về giao diện tại các trường validate
- Fix lỗi về service tại các API có validate dữ liệu đầu vào
- Fix lỗi token không tự refresh sau mỗi 15p tại client
- Fix lỗi workflow không thể gửi được request đến finesse
- Fix lỗi webhook không xử lý dược request có dạng tham số truyền vào
- Fix lỗi đồng bộ file DLL khi thực hiện HA
- Fix lỗi không tìm được file dll khi triển khai trên môi trường linux / docker
- Fix lỗi show history API

## 2.1.0 (2024-02-01)
### Features
- Khởi tạo
- Chức năng quản lý workflow
- Chức năng cấu hình workflow
- Chức năng deploy workflow sang API
- Chức năng quản lý tài nguyên kết nối SQL, Email
- Chức năng quản lý tài nguyên object, DLL
- Bổ sung chức năng cho phép gọi XML và chuyển từ XML sang object để xử lý.

### Bug Fix
- Fix lỗi HA, đồng bộ file DLL
- Fix lỗi ANBM, giới hạn thời gian sử dụng của session và refesh token
- Fix lỗi đăng ký tài khoản phải sử dụng mật khẩu theo policy
- Fix lỗi expose API từ workflow.
- Fix lỗi sau khi thực hiện quét sonar.
