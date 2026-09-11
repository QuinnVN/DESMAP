# **D: DESIRE & CAREER ORIENTATION**

### **Mục đích**

D đo **những điều người chơi mong muốn và coi trọng khi lựa chọn nghề nghiệp**.  
D trả lời câu hỏi (giá trị nghề nghiệp) 

> **“Nếu chọn một nghề, tôi muốn nghề đó mang lại điều gì cho mình?”**

D **không đo năng lực** và **không đo ngành người chơi yêu thích**. Phần ngành yêu thích sẽ được xác định ở bước **Career Interest** trước DESMAP.  
Theo hệ thống **O\*NET** (Mạng lưới thông tin nghề nghiệp do Bộ Lao động Hoa Kỳ phát triển), các giá trị nghề nghiệp (**Work Values**, hay còn gọi là *Work Importance Locator*) được chia thành 6 nhóm chính 

## **1\. Cấu trúc D**

D gồm **6 sub-dimensions**, mỗi nhóm **2 câu**, tổng cộng **12 câu**.

| Mã | Dimension | Nội dung |
| ----- | ----- | ----- |
| **D1** | Achievement  | Thu nhập, phúc lợi, sự ổn định |
| **D2** | Support  | Học hỏi, phát triển, thành tựu, thử thách |
| **D3** | Independence  | Tự chủ, tự quyết định cách làm việc |
| **D4** | Relationships  | Ý nghĩa, đóng góp, tạo giá trị |
| **D5** | Recognition | Công nhận, địa vị, tiếng nói, ảnh hưởng |
| **D6** | Working Conditions  | Cân bằng công việc – cuộc sống |

# **2\. Logic chấm điểm**

Mỗi câu có **3 lựa chọn**.

* Lựa chọn thể hiện **mong muốn cao** → `+2`  
* Lựa chọn trung gian → `+1`  
* Lựa chọn thể hiện **mong muốn thấp** → `+0`

Mỗi dimension có 2 câu:

> **Điểm tối đa mỗi D \= 4**

Quy đổi:

| Điểm | % |
| ----- | ----- |
| 0/4 | 0% |
| 1/4 | 25% |
| 2/4 | 50% |
| 3/4 | 75% |
| 4/4 | 100% |

**Lưu ý cho IT:** không nên hard-code rằng A luôn \= 2, B \= 1, C \= 0\. Có thể đảo vị trí đáp án để tránh người chơi đoán pattern. Database chỉ cần lưu:

option → dimension → score

**Bộ 12 câu hỏi**  
**D1 — Achievement: Thành tựu**

### D1.1

Nếu được chọn giữa ba công việc có mức lương và điều kiện tương đương, bạn dễ bị thu hút nhất bởi công việc nào?

A. Công việc cho tôi cơ hội làm những nhiệm vụ khó và nhìn thấy rõ mình tiến bộ qua từng lần. → `D1 +2`  
 B. Công việc có một số mục tiêu để phấn đấu nhưng không tạo áp lực phải liên tục vượt qua chính mình. → `D1 +1`  
 C. Công việc quen thuộc, ổn định và không đặt nặng việc phải đạt thêm thành tích mới. → `D1 +0`

### D1.2

Sau một thời gian làm việc, điều nào khiến bạn cảm thấy công sức của mình đáng giá nhất?

A. Tôi nhìn thấy một kết quả cụ thể và biết rằng mình đã làm được điều trước đây chưa làm được. → `D1 +2`  
 B. Tôi hoàn thành tốt phần việc được giao và đáp ứng yêu cầu. → `D1 +1`  
 C. Công việc diễn ra thuận lợi và tôi không phải đặt nặng việc mình đạt được thành tựu gì. → `D1 +0`

## **D2 — Support: Sự hỗ trợ từ tổ chức**

### D2.1

Nếu mới vào một nơi làm việc và gặp một nhiệm vụ khó, môi trường nào khiến bạn muốn gắn bó hơn?

A. Có người hướng dẫn rõ ràng, sẵn sàng góp ý và giúp tôi phát triển khi gặp khó khăn. → `D2 +2`  
 B. Tôi có thể hỏi khi cần, nhưng phần lớn phải tự tìm cách xử lý. → `D2 +1`  
 C. Mỗi người chủ yếu tự chịu trách nhiệm cho công việc của mình và ít cần sự hỗ trợ từ tổ chức. → `D2 +0`

### D2.2

Hai công ty đưa ra công việc gần giống nhau. Bạn có xu hướng chọn nơi nào hơn?

A. Nơi có quản lý quan tâm đến sự phát triển của nhân viên và có cơ chế hỗ trợ khi gặp vấn đề. → `D2 +2`  
 B. Nơi có hỗ trợ cơ bản nhưng nhân viên vẫn phải chủ động phần lớn. → `D2 +1`  
 C. Nơi trao công việc rồi để nhân viên tự xử lý, miễn là kết quả đạt yêu cầu. → `D2 +0`

## **D3 — Independence: Tự chủ**

### D3.1

Khi được giao một nhiệm vụ, cách làm việc nào khiến bạn thoải mái nhất?

A. Biết mục tiêu cần đạt rồi tự quyết định cách thực hiện. → `D3 +2`  
 B. Có một khung hướng dẫn chung nhưng vẫn được tự điều chỉnh một số phần. → `D3 +1`  
 C. Có quy trình và hướng dẫn cụ thể để làm theo từng bước. → `D3 +0`

### D3.2

Nếu cả ba công việc đều cho kết quả tương đương, bạn thích môi trường nào hơn?

A. Tôi được thử cách làm và ý tưởng riêng miễn là chịu trách nhiệm về kết quả. → `D3 +2`  
 B. Tôi được đề xuất thay đổi nhưng cần trao đổi với người phụ trách trước. → `D3 +1`  
 C. Tôi làm theo cách đã được thống nhất để mọi người vận hành đồng đều. → `D3 +0`

## **D4 — Relationships: Quan hệ và giá trị xã hội**

### **D4.1**

Nếu phải chọn một công việc để gắn bó lâu dài, yếu tố nào dễ khiến bạn cảm thấy công việc đó “có ý nghĩa” hơn?

A. Tôi thấy công việc của mình tạo ra giá trị tích cực cho người khác hoặc cộng đồng. → `D4 +2`  
 B. Công việc có ích ở một mức nhất định, dù đó không phải điều tôi nghĩ đến thường xuyên. → `D4 +1`  
 C. Tôi chủ yếu quan tâm công việc có phù hợp với nhu cầu cá nhân của mình hay không. → `D4 +0`

### **D4.2**

Giả sử một công việc rất phù hợp với năng lực của bạn nhưng sản phẩm mà công ty tạo ra không thật sự phù hợp với những giá trị bạn tin tưởng. Bạn có xu hướng:

A. Cân nhắc nghiêm túc việc chọn một nơi khác dù công việc hiện tại khá phù hợp. → `D4 +2`  
 B. Vẫn có thể làm nếu điểm không phù hợp đó không quá lớn. → `D4 +1`  
 C. Tách quan điểm cá nhân khỏi công việc, miễn là công việc đáp ứng những điều tôi cần. → `D4 +0`

## **D5 — Recognition: Sự công nhận**

### **D5.1**

Bạn vừa hoàn thành một phần việc rất tốt. Điều nào khiến bạn hài lòng hơn?

A. Thành quả của tôi được ghi nhận và mọi người biết tôi đã đóng góp như thế nào. → `D5 +2`  
 B. Tôi được ghi nhận khi có dịp, nhưng không nhất thiết phải được chú ý nhiều. → `D5 +1`  
 C. Tôi biết mình làm tốt là đủ, người khác có biết hay không không quá quan trọng. → `D5 +0`

### **D5.2**

Nếu phải chọn giữa ba hướng phát triển nghề nghiệp, bạn nghiêng về hướng nào hơn?

A. Có cơ hội tiến lên vị trí cao hơn, được giao nhiều quyền hạn và có tiếng nói lớn hơn. → `D5 +2`  
 B. Có thể thăng tiến nếu phù hợp, nhưng tôi không đặt đó là mục tiêu chính. → `D5 +1`  
 C. Tôi không cần vị trí cao hơn nếu vẫn được làm công việc mình thấy phù hợp. → `D5 +0`

## **D6 — Working Conditions: Điều kiện làm việc**

### **D6.1**

Nếu có hai công việc đều khá phù hợp với bạn, điều nào dễ làm bạn nghiêng về một công việc hơn?

A. Thu nhập, sự ổn định và các điều kiện làm việc đáp ứng tốt nhu cầu cuộc sống của tôi. → `D6 +2`  
 B. Các điều kiện trên ở mức ổn là được, tôi còn cân nhắc nhiều yếu tố khác. → `D6 +1`  
 C. Tôi có thể chấp nhận điều kiện vật chất kém thuận lợi hơn nếu bản thân thực sự thích công việc. → `D6 +0`

### **D6.2**

Bạn được mời làm một công việc rất thú vị nhưng thu nhập chưa ổn định và môi trường làm việc còn nhiều bất tiện. Bạn có xu hướng:

A. Cân nhắc khá kỹ vì sự ổn định và điều kiện làm việc ảnh hưởng lớn đến quyết định của tôi. → `D6 +2`  
 B. Có thể thử nếu những điểm hấp dẫn khác đủ lớn. → `D6 +1`  
 C. Sẵn sàng chấp nhận những bất tiện đó nếu công việc khiến tôi hứng thú. **→ `D6 +0`**  
**4\. Output cho người chơi**  
Ví dụ:

> **D1 \- Financial & Security: 50%**  
> **D2 \- Growth & Achievement: 83%**  
> **D3 \- Autonomy: 100%**  
> **D4 \- Meaning & Contribution: 67%**  
> **D5 \- Recognition & Status: 50%**  
> **D6 \- Lifestyle & Balance: 83%**

AI sau đó đọc **pattern 6 chỉ số**, ví dụ:

> **Dominant desires:** Autonomy \+ Growth \+ Lifestyle  
> Người chơi có xu hướng tìm kiếm một nghề cho phép phát triển, tự chủ trong cách làm việc nhưng vẫn duy trì chất lượng cuộc sống.

# **E \- EXPERTISE**

Dựa vào Các nền tảng lý thuyết gốc

* **Cơ sở phân loại:** cấu trúc kỹ năng trong tài liệu O*NET gồm* ***Basic Skills*** *và* ***Cross-Functional Skills***\*; nhóm thứ hai gồm năm nhánh còn lại trong bảng. \*

**E đo dạng năng lực mà người chơi có xu hướng phát huy trong công việc.**

| Mã | Nhóm E | Nội dung |
| ----- | ----- | ----- |
| **E1** | **Basic Skills \- Kỹ năng nền tảng** | Đọc hiểu, lắng nghe, diễn đạt, tính toán, học hỏi và tư duy phản biện. |
| **E2** | **Complex Problem Solving — Giải quyết vấn đề phức tạp** | Nhận diện vấn đề, xem xét thông tin, xây dựng và đánh giá phương án giải quyết. |
| **E3** | **Social Skills — Kỹ năng tương tác xã hội** | Phối hợp, thuyết phục, thương lượng, hướng dẫn và hiểu phản ứng của người khác. |
| **E4** | **Technical Skills — Kỹ năng kỹ thuật** | Lựa chọn, vận hành, kiểm tra và xử lý sự cố của công cụ, thiết bị hoặc hệ thống kỹ thuật. |
| **E5** | **Systems Skills — Kỹ năng hệ thống** | Hiểu quan hệ giữa các bộ phận, đánh giá tác động của thay đổi và cải thiện cách hệ thống hoạt động. |
| **E6** | **Resource Management Skills — Kỹ năng quản lý nguồn lực** | Phân bổ thời gian, tiền bạc, vật tư và nhân sự. |

**Mỗi nhóm 2 câu. Mỗi câu 3 lựa chọn.**

### **Logic chấm điểm E**

* Mức thể hiện năng lực cao → `+2`  
* Mức thể hiện trung gian → `+1`  
* Mức thể hiện thấp → `+0`

Mỗi dimension có **2 câu × 2 điểm \= tối đa 4 điểm**.

> **Điểm từng E \= (điểm đạt được / 4\) × 100**

**3\. Bộ 12 câu**  
**E1 — Basic Skills: Kỹ năng nền tảng**

### **E1.1**

Bạn nhận được một hướng dẫn dài cho một nhiệm vụ mới và chỉ có ít thời gian để đọc. Bạn thường:

A. Xác định mục tiêu, các yêu cầu chính và những điều cần chú ý trước khi bắt đầu. → `E1 +2`  
 B. Đọc toàn bộ rồi bắt đầu làm, nếu gặp chỗ chưa rõ thì quay lại kiểm tra. → `E1 +1`  
 C. Bắt đầu từ những phần mình hiểu trước và tìm thêm hướng dẫn khi gặp khó khăn. → `E1 +0`

### E1.2

Nhóm đang chuẩn bị một hoạt động với ngân sách giới hạn. Khi cần kiểm tra các khoản chi, bạn thường:

A. Tự tính tổng chi phí, tỉ lệ hoặc phần chênh lệch để kiểm tra xem kế hoạch có nằm trong ngân sách không. → `E1 +2`  
 B. Tính những khoản chính rồi dùng công cụ hoặc nhờ người khác kiểm tra lại. → `E1 +1`  
 C. Ưu tiên phụ trách phần nội dung khác và để người quen xử lý số liệu đảm nhận việc tính toán. → `E1 +0`

## **E2 — Complex Problem Solving: Giải quyết vấn đề phức tạp**

### E2.1

Một dự án đang gặp cùng lúc ba vấn đề: thiếu thời gian, thiếu người và kết quả chưa đạt yêu cầu. Bạn thường:

A. Xác định vấn đề nào đang ảnh hưởng lớn nhất, tìm nguyên nhân rồi xử lý theo thứ tự ưu tiên. → `E2 +2`  
 B. Chọn vấn đề cấp bách nhất để giải quyết trước rồi xem tình hình thay đổi thế nào. → `E2 +1`  
 C. Bắt đầu xử lý vấn đề mình thấy dễ giải quyết nhất để dự án tiếp tục tiến triển. → `E2 +0`

### E2.2

Bạn có ba phương án để giải quyết cùng một vấn đề nhưng mỗi phương án đều có ưu và nhược điểm. Bạn thường:

A. Đặt ra các tiêu chí cần đạt, so sánh từng phương án theo các tiêu chí rồi mới quyết định. → `E2 +2`  
 B. So sánh những ưu và nhược điểm nổi bật nhất rồi chọn phương án có vẻ phù hợp. → `E2 +1`  
 C. Ưu tiên phương án quen thuộc hoặc từng có hiệu quả trong tình huống tương tự. → `E2 +0`

## **E3 — Social Skills: Kỹ năng tương tác xã hội**

### E3.1

Phần việc của bạn chỉ có thể hoàn thành sau khi một thành viên khác gửi dữ liệu, nhưng tiến độ của họ đang chậm. Bạn thường:

A. Trao đổi để biết tình hình, thống nhất thời điểm bàn giao và điều chỉnh phần việc hai bên để không ảnh hưởng tiến độ chung. → `E3 +2`  
 B. Nhắc họ về thời hạn và chuẩn bị trước những phần mình có thể làm. → `E3 +1`  
 C. Hoàn thành những phần của mình trước và chờ dữ liệu được gửi sang rồi xử lý tiếp. → `E3 +0`

### E3.2

Bạn đề xuất một ý tưởng nhưng một số thành viên trong nhóm không đồng ý. Bạn thường:

A. Tìm hiểu điều họ lo ngại, điều chỉnh cách giải thích và đưa ra lý do liên quan trực tiếp đến vấn đề đó. → `E3 +2`  
 B. Giải thích rõ hơn lợi ích của ý tưởng và cố gắng trả lời các phản đối chính. → `E3 +1`  
 C. Trình bày quan điểm của mình một lần rõ ràng rồi để cả nhóm tự quyết định. → `E3 +0`

## **E4 — Technical Skills: Kỹ năng kỹ thuật**

### E4.1

Bạn phải sử dụng một thiết bị hoặc phần mềm đã từng được hướng dẫn nhưng đã lâu không dùng. Bạn thường:

A. Nhớ lại nguyên lý/các bước chính, thử thao tác và kiểm tra kết quả để tự điều chỉnh nếu cần. → `E4 +2`  
 B. Xem nhanh lại hướng dẫn hoặc ví dụ rồi làm theo. → `E4 +1`  
 C. Muốn có người quen với thiết bị hoặc phần mềm đó hướng dẫn lại trước khi bắt đầu. → `E4 +0`

### E4.2

Một thiết bị hoặc phần mềm quen thuộc đột nhiên không hoạt động như bình thường. Việc đầu tiên bạn thường làm là:

A. Quan sát dấu hiệu lỗi, kiểm tra từng nguyên nhân có khả năng xảy ra để xác định vấn đề. → `E4 +2`  
 B. Thử một số cách xử lý thường dùng như khởi động lại, kiểm tra cài đặt hoặc kết nối. → `E4 +1`  
 C. Tìm hướng dẫn hoặc nhờ một người có kinh nghiệm kiểm tra để tránh làm sai thêm. → `E4 +0`

## **E5 — Systems Skills: Kỹ năng hệ thống**

### **E5.1**

Một dự án gồm nhiều công đoạn liên tiếp. Một công đoạn ở giữa bị trễ hai ngày. Bạn thường:

A. Xem công đoạn đó liên quan đến những phần nào phía sau và đánh giá toàn bộ tiến độ sẽ bị ảnh hưởng ra sao. → `E5 +2`  
 B. Kiểm tra những nhiệm vụ ngay sau công đoạn đó để điều chỉnh lịch làm việc. → `E5 +1`  
 C. Tập trung giúp công đoạn đang chậm hoàn thành nhanh hơn rồi mới xử lý các vấn đề phát sinh tiếp theo. → `E5 +0`

### E5.2

Nhóm muốn thay đổi cách tổ chức công việc để tiết kiệm thời gian nhưng cách mới cần thêm chi phí và thời gian làm quen. Bạn thường:

A. So sánh lợi ích, chi phí và ảnh hưởng của thay đổi đến toàn bộ quá trình trước khi quyết định. → `E5 +2`  
 B. Xem cách mới có giúp giải quyết vấn đề chính hay không rồi cân nhắc triển khai. → `E5 +1`  
 C. Ưu tiên giữ cách hiện tại nếu nó vẫn hoạt động tương đối ổn định. → `E5 +0`

## **E6 — Resource Management Skills: Kỹ năng quản lý nguồn lực**

### **E6.1**

Bạn có ba nhiệm vụ cùng hạn trong tuần nhưng không đủ thời gian để làm tất cả cùng lúc. Bạn thường:

A. Ước lượng thời gian và mức độ ưu tiên của từng việc rồi lập thứ tự thực hiện cụ thể. → `E6 +2`  
 B. Làm việc có hạn gần nhất trước rồi chuyển sang những việc còn lại. → `E6 +1`  
 C. Bắt đầu từ việc mình dễ tập trung hoặc làm nhanh nhất để giảm bớt lượng việc. → `E6 +0`

### E6.2

Khi được giao phân công một dự án nhóm, bạn thường:

A. Xem yêu cầu từng nhiệm vụ và điểm mạnh của từng thành viên rồi phân công sao cho phù hợp. → `E6 +2`  
 B. Chia công việc tương đối đều và điều chỉnh lại nếu có người gặp khó khăn. → `E6 +1`  
 C. Để mọi người tự chọn phần mình muốn làm rồi xử lý những phần còn thiếu sau. → `E6 +0`

#  **S: SOCIAL ROLE**

### **Mục đích**

S đo **cách một cá nhân có xu hướng đảm nhận vai trò khi làm việc và tương tác với người khác**.  
S trả lời câu hỏi:

> **“Khi làm việc với người khác, tôi có xu hướng đóng vai trò gì?”**

S **không đo tính cách hướng nội/hướng ngoại** và **không đo năng lực giao tiếp**. S tập trung vào **vai trò chức năng mà cá nhân thường đảm nhận trong một nhóm**.  
Cơ sở lý thuyết có thể dựa trên **Functional/Team Roles**, đặc biệt là phân loại **Task Roles, Maintenance Roles và Individual Roles** của **Benne & Sheats (1948)**.

# **1\. Cấu trúc S**

S gồm **3 Social Role chính**, bên trong mỗi nhóm có các **sub-roles**.

| Mã | Social Role chính | Sub-roles |
| ----- | ----- | ----- |
| **S1** | **Task-oriented Roles** | Initiator, Information Giver, Coordinator, Evaluator-Critic |
| **S2** | **Maintenance / Social Roles** | Encourager/Energizer, Harmonizer/Mediator, Gatekeeper |
| **S3** | **Individual-oriented Roles** | Dominator, Recognition Seeker, Independent Contributor |

### **S1 — Task-oriented Roles**

Tập trung vào **hoàn thành nhiệm vụ, giải quyết vấn đề và đạt mục tiêu chung**.

* **Initiator:** Đề xuất ý tưởng hoặc hướng đi mới.  
* **Information Giver:** Cung cấp thông tin, kiến thức và kinh nghiệm.  
* **Coordinator:** Kết nối và điều phối các phần việc.  
* **Evaluator-Critic:** Phân tích, phản biện và đánh giá chất lượng.

### **S2 — Maintenance / Social Roles**

Tập trung vào **duy trì quan hệ, động lực và sự phối hợp giữa các thành viên**.

* **Encourager / Energizer:** Khích lệ và tạo động lực.  
* **Harmonizer / Mediator:** Hòa giải và giảm xung đột.  
* **Gatekeeper:** Điều tiết sự tham gia, tạo cơ hội để các thành viên lên tiếng.

### **S3 — Individual-oriented Roles**

Tập trung vào **cách cá nhân thể hiện vị trí và nhu cầu của mình trong nhóm**.

* **Dominator:** Có xu hướng kiểm soát hoặc áp đặt hướng đi.  
* **Recognition Seeker:** Tìm kiếm sự công nhận cho đóng góp cá nhân.  
* **Independent Contributor:** Ưu tiên tự chủ và đóng góp độc lập.

> **Lưu ý:** S3 không nên được diễn giải như một nhóm “tiêu cực”. Các sub-role chỉ mô tả **xu hướng hành vi**, không phải đánh giá đạo đức hay tính cách.

# **2\. Cấu trúc bài test**

S gồm **20 câu hỏi**.

* Mỗi sub-role có **2 câu**.  
* Mỗi câu có **3 lựa chọn**.  
* Mỗi lựa chọn tương ứng với mức độ thể hiện sub-role:  
  * Cao → `+2`  
  * Trung gian → `+1`  
  * Thấp → `+0`

Tổng:

> **10 sub-roles × 2 câu \= 20 câu**

# **3\. Logic chấm điểm**

Mỗi câu chỉ đóng góp điểm cho **một sub-role**.  
Ví dụ:

### **Câu đo Coordinator**

**A.** Chủ động sắp xếp và kết nối các phần việc để mọi người phối hợp. → `Coordinator +2`  
**B.** Phối hợp với các thành viên khi cần. → `Coordinator +1`  
**C.** Chỉ tập trung hoàn thành phần việc của mình. → `Coordinator +0`

# **4\. Bộ 20 câu**

## **S1 — TASK-ORIENTED ROLES**

### **S1.1 — Initiator**

Khi nhóm bắt đầu một dự án nhưng chưa biết nên làm theo hướng nào, bạn thường:  
**A.** Đề xuất một vài hướng mới để nhóm bắt đầu thảo luận. → `Initiator +2`  
**B.** Chờ mọi người đưa ra ý tưởng rồi đóng góp thêm nếu cần. → `Initiator +1`  
**C.** Chờ nhóm thống nhất hướng đi rồi mới bắt đầu phần việc của mình. → `Initiator +0`

### **S1.2 — Initiator**

Nếu bạn nhận thấy cách nhóm đang làm chưa hiệu quả, bạn sẽ:  
**A.** Chủ động đề xuất một cách tiếp cận khác. → `Initiator +2`  
**B.** Gợi ý thay đổi một vài điểm trong cách làm hiện tại. → `Initiator +1`  
**C.** Tiếp tục làm theo cách nhóm đã thống nhất. → `Initiator +0`

### **S1.3 — Information Giver**

Khi nhóm đang thảo luận một vấn đề mà bạn có kiến thức liên quan, bạn thường:  
**A.** Chủ động cung cấp thông tin và giải thích những điểm quan trọng. → `Information Giver +2`  
**B.** Chia sẻ thông tin khi có người hỏi hoặc khi thấy cần thiết. → `Information Giver +1`  
**C.** Để người khác tự tìm hiểu nếu vấn đề không thuộc phần việc của mình. → `Information Giver +0`

### **S1.4 — Information Giver**

Khi nhóm thiếu dữ liệu để đưa ra quyết định, bạn có xu hướng:  
**A.** Tìm kiếm và cung cấp thông tin cần thiết cho cả nhóm. → `Information Giver +2`  
**B.** Tìm thông tin liên quan đến phần việc của mình. → `Information Giver +1`  
**C.** Chờ người phụ trách thông tin giải quyết vấn đề. → `Information Giver +0`

### **S1.5 — Coordinator**

Khi một dự án có nhiều người cùng tham gia, bạn thường:  
**A.** Chủ động phân chia và kết nối các phần việc để mọi người phối hợp. → `Coordinator +2`  
**B.** Theo dõi phần việc của mình và phối hợp khi cần. → `Coordinator +1`  
**C.** Tập trung hoàn thành phần việc được giao. → `Coordinator +0`

### **S1.6 — Coordinator**

Nếu một thành viên hoàn thành công việc nhưng phần việc đó chưa kết nối được với phần của những người khác, bạn sẽ:  
**A.** Tìm cách sắp xếp lại các phần việc để chúng liên kết với nhau. → `Coordinator +2`  
**B.** Trao đổi với những người liên quan để tìm cách phối hợp. → `Coordinator +1`  
**C.** Tiếp tục phần việc của mình và để người phụ trách xử lý. → `Coordinator +0`

### **S1.7 — Evaluator-Critic**

Khi nhóm đưa ra một ý tưởng mới, điều bạn thường chú ý nhất là:  
**A.** Kiểm tra xem ý tưởng có hợp lý, khả thi và hiệu quả hay không. → `Evaluator-Critic +2`  
**B.** Cân nhắc ưu điểm và hạn chế của ý tưởng. → `Evaluator-Critic +1`  
**C.** Quan tâm nhiều hơn đến việc mọi người có thích ý tưởng đó hay không. → `Evaluator-Critic +0`

### **S1.8 — Evaluator-Critic**

Khi xem lại sản phẩm của nhóm trước khi nộp, bạn thường:  
**A.** Tìm những lỗi, điểm thiếu logic hoặc điểm có thể cải thiện. → `Evaluator-Critic +2`  
**B.** Kiểm tra những lỗi rõ ràng và những yêu cầu chính. → `Evaluator-Critic +1`  
**C.** Tin rằng phần việc đã hoàn thành thì không cần kiểm tra quá nhiều. → `Evaluator-Critic +0`

# **S2 — MAINTENANCE / SOCIAL ROLES**

### **S2.1 — Encourager / Energizer**

Khi nhóm bắt đầu mất động lực vì công việc khó, bạn thường:  
**A.** Chủ động động viên và giúp mọi người lấy lại tinh thần. → `Encourager +2`  
**B.** Động viên một vài thành viên nếu thấy họ thực sự cần. → `Encourager +1`  
**C.** Tập trung vào phần việc của mình. → `Encourager +0`

### **S2.2 — Encourager / Energizer**

Khi một thành viên làm tốt một phần việc, bạn thường:  
**A.** Chủ động ghi nhận và khích lệ họ. → `Encourager +2`  
**B.** Ghi nhận nếu thành tích đó thực sự nổi bật. → `Encourager +1`  
**C.** Cho rằng hoàn thành tốt công việc là điều bình thường. → `Encourager +0`

### **S2.3 — Harmonizer / Mediator**

Khi hai thành viên trong nhóm xảy ra bất đồng, bạn thường:  
**A.** Tìm hiểu quan điểm của cả hai và giúp họ tìm điểm chung. → `Harmonizer +2`  
**B.** Đưa ra ý kiến nếu cuộc tranh luận bắt đầu ảnh hưởng đến nhóm. → `Harmonizer +1`  
**C.** Để hai người tự giải quyết vấn đề của họ. → `Harmonizer +0`

### **S2.4 — Harmonizer / Mediator**

Nếu một cuộc tranh luận trong nhóm trở nên căng thẳng, bạn sẽ:  
**A.** Chủ động làm dịu tình hình và đưa cuộc thảo luận trở lại vấn đề. → `Harmonizer +2`  
**B.** Chờ mọi người bình tĩnh rồi tiếp tục thảo luận. → `Harmonizer +1`  
**C.** Không can thiệp nếu vấn đề không trực tiếp liên quan đến mình. → `Harmonizer +0`

### **S2.5 — Gatekeeper**

Trong một cuộc họp, bạn nhận thấy một thành viên gần như không có cơ hội phát biểu. Bạn sẽ:  
**A.** Chủ động mời họ chia sẻ quan điểm. → `Gatekeeper +2`  
**B.** Chờ xem họ có muốn tham gia hay không. → `Gatekeeper +1`  
**C.** Tiếp tục cuộc thảo luận vì họ có thể tự lên tiếng nếu muốn. → `Gatekeeper +0`

### **S2.6 — Gatekeeper**

Khi một người liên tục nói và khiến những người khác khó tham gia, bạn thường:  
**A.** Chủ động điều tiết cuộc trò chuyện để tạo cơ hội cho những người khác. → `Gatekeeper +2`  
**B.** Nhắc nhẹ nếu cuộc trò chuyện bắt đầu mất cân bằng. → `Gatekeeper +1`  
**C.** Để người dẫn cuộc họp tự xử lý. → `Gatekeeper +0`

# **S3 — INDIVIDUAL-ORIENTED ROLES**

### **S3.1 — Dominator**

Khi nhóm không thống nhất được cách làm, bạn thường:  
**A.** Chủ động quyết định hướng đi và yêu cầu mọi người thực hiện. → `Dominator +2`  
**B.** Đưa ra quan điểm mạnh và cố gắng thuyết phục nhóm theo hướng đó. → `Dominator +1`  
**C.** Chờ nhóm thảo luận và cùng thống nhất quyết định. → `Dominator +0`

### **S3.2 — Dominator**

Nếu bạn tin rằng phương án của mình tốt nhất nhưng nhóm đang chọn phương án khác:  
**A.** Tôi sẽ cố gắng giành quyền quyết định để nhóm làm theo phương án của mình. → `Dominator +2`  
**B.** Tôi sẽ cố gắng thuyết phục nhóm thêm một lần nữa. → `Dominator +1`  
**C.** Tôi chấp nhận quyết định chung nếu nhóm đã thống nhất. → `Dominator +0`

### **S3.3 — Recognition Seeker**

Sau khi hoàn thành một nhiệm vụ quan trọng, điều khiến bạn hài lòng nhất là:  
**A.** Mọi người biết và ghi nhận đóng góp của tôi. → `Recognition Seeker +2`  
**B.** Được ghi nhận nếu tôi thực sự làm tốt. → `Recognition Seeker +1`  
**C.** Biết mình đã hoàn thành tốt là đủ. → `Recognition Seeker +0`

### **S3.4 — Recognition Seeker**

Nếu một ý tưởng của bạn đóng góp lớn vào thành công của nhóm nhưng không ai biết đó là ý tưởng của bạn:  
**A.** Tôi sẽ muốn mọi người biết đóng góp đó là của mình. → `Recognition Seeker +2`  
**B.** Tôi hơi tiếc nhưng vẫn chấp nhận được. → `Recognition Seeker +1`  
**C.** Tôi không quan tâm miễn là nhóm đạt kết quả tốt. → `Recognition Seeker +0`

### **S3.5 — Independent Contributor**

Khi được giao một phần việc lớn trong dự án, bạn thích:  
**A.** Được tự quyết định cách làm và hoàn thành phần việc độc lập. → `Independent Contributor +2`  
**B.** Có một số trao đổi với nhóm nhưng vẫn tự xử lý phần lớn công việc. → `Independent Contributor +1`  
**C.** Làm việc sát với các thành viên khác trong suốt quá trình. → `Independent Contributor +0`

### **S3.6 — Independent Contributor**

Nếu có thể lựa chọn giữa:  
**A.** Một nhiệm vụ bạn có thể tự chịu trách nhiệm từ đầu đến cuối. → `Independent Contributor +2`  
**B.** Một nhiệm vụ kết hợp giữa làm độc lập và phối hợp. → `Independent Contributor +1`  
**C.** Một nhiệm vụ yêu cầu liên tục trao đổi và phối hợp với người khác. → `Independent Contributor +0`

# **5\. Logic tổng hợp kết quả**

Đây là phần **quan trọng nhất để IT implement**.

### **Bước 1 \- Tính điểm sub-role**

Mỗi sub-role có:

> **2 câu × 2 điểm \= tối đa 4 điểm**

Ví dụ:  
Initiator \= 3/4  
Information Giver \= 2/4  
Coordinator \= 4/4  
Evaluator-Critic \= 3/4

### **Bước 2 — Tính điểm của 3 Social Roles chính**

**Không cộng điểm thô**, vì S1 có 4 sub-role trong khi S2 và S3 chỉ có 3\.  
Tính:

> **S1 \= (Initiator \+ Information Giver \+ Coordinator \+ Evaluator-Critic) / 16 × 100**

> **S2 \= (Encourager \+ Harmonizer \+ Gatekeeper) / 12 × 100**

> **S3 \= (Dominator \+ Recognition Seeker \+ Independent Contributor) / 12 × 100**

Ví dụ:

| Sub-role | Score |
| ----- | ----- |
| Initiator | 3/4 |
| Information Giver | 2/4 |
| Coordinator | 4/4 |
| Evaluator-Critic | 3/4 |
| Encourager | 2/4 |
| Harmonizer | 3/4 |
| Gatekeeper | 2/4 |
| Dominator | 1/4 |
| Recognition Seeker | 2/4 |
| Independent Contributor | 3/4 |

→  
**S1 \= 12/16 \= 75%**  
**S2 \= 7/12 \= 58.3%**  
**S3 \= 6/12 \= 50%**  
→ **Kết luận: S1 \- Task-oriented Role**

### **Bước 3 — Xác định sub-role nổi trội**

Sau khi xác định S chính, hệ thống tìm **sub-role có điểm cao nhất trong S đó**.  
Ví dụ:

> **Primary Social Role: S1 \- Task-oriented**  
> **Dominant Sub-role: Coordinator**

→ AI mô tả người chơi là người có xu hướng **đưa công việc tiến về phía trước thông qua việc kết nối và điều phối các thành viên**.

# **6\. Output cho người chơi**

Ví dụ:

### **Your Social Role**

**S1 — Task-oriented Role**

> Bạn có xu hướng tập trung vào việc **đưa nhóm đạt mục tiêu, giải quyết vấn đề và tổ chức công việc**.

**Your dominant sub-role: Coordinator**

> Bạn đặc biệt có xu hướng kết nối các phần việc và giúp các thành viên phối hợp để công việc vận hành thống nhất.

### **Social Role Profile**

| Role | Score |
| ----- | ----- |
| **S1 — Task-oriented** | **75%** |
| S2 — Maintenance / Social | 58% |
| S3 — Individual-oriented | 50% |

### **Sub-role Profile**

| Sub-role | Score |
| ----- | ----- |
| Coordinator | 100% |
| Initiator | 75% |
| Evaluator-Critic | 75% |
| Information Giver | 50% |
|  |  |

**Như vậy, kết quả cuối cùng của S chỉ có 3 hướng chính: S1 / S2 / S3.** Các sub-role không phải kết quả độc lập mà dùng để **giải thích người chơi thuộc S1/S2/S3 theo kiểu nào**.

# **M — MIND**

### **Mục đích**

M đánh giá **cách người dùng vận dụng trí tuệ để phân tích vấn đề, tạo ra ý tưởng và xử lý tình huống thực tế**.  
M trả lời câu hỏi:

> **“Tôi thường sử dụng trí tuệ theo cách nào để giải quyết vấn đề?”**

M không đo kiến thức chuyên môn, sở thích nghề nghiệp hay toàn bộ trí thông minh của một cá nhân.

### **Cơ sở lý thuyết**

M được xây dựng dựa trên **Lý thuyết trí tuệ ba thành phần** (*Triarchic Theory of Human Intelligence*) do Robert J. Sternberg đề xuất trong *Beyond IQ* năm 1985 và được phát triển tiếp thành **Theory of Successful Intelligence**.  
Theo hướng phát triển này, việc giải quyết vấn đề và đạt mục tiêu đòi hỏi sự phối hợp của ba dạng năng lực:

* **Analytical:** phân tích và đánh giá ý tưởng;  
* **Creative:** tạo ra và xử lý ý tưởng mới;  
* **Practical:** áp dụng ý tưởng trong bối cảnh thực tế.

Sternberg nhấn mạnh rằng ba dạng năng lực này phối hợp với nhau, không nhất thiết tồn tại như ba kiểu người hoàn toàn tách biệt.

## **1\. Cấu trúc M**

M gồm **3 dimensions**:

| Mã | Dimension | Nội dung |
| ----- | ----- | ----- |
| **M1** | **Analytical Intelligence — Trí thông minh phân tích** | Phân tích thông tin, so sánh phương án, phát hiện điểm chưa hợp lý và đánh giá bằng chứng. |
| **M2** | **Creative Intelligence — Trí thông minh sáng tạo** | Tạo ra ý tưởng mới, nhìn vấn đề từ góc độ khác và xử lý tình huống chưa quen thuộc. |
| **M3** | **Practical Intelligence — Trí thông minh thực tiễn** | Áp dụng kiến thức vào tình huống thực tế, nhận biết yêu cầu của hoàn cảnh và điều chỉnh cách xử lý để đạt mục tiêu. |

## **3\. Bộ 12 câu**

### **M1 — Analytical Intelligence: Trí thông minh phân tích**

#### **M1.1**

Khi hai nguồn đưa ra kết luận trái ngược về cùng một vấn đề, bạn thường:  
**A.** Kiểm tra bằng chứng, nguồn dữ liệu và cách lập luận của cả hai trước khi kết luận. → `M1 +2`  
**B.** So sánh nội dung chính rồi chọn nguồn có vẻ đáng tin hơn. → `M1 +1`  
**C.** Chọn nguồn quen thuộc hoặc được nhiều người tin hơn. → `M1 +0`

#### **M1.2**

Khi gặp một vấn đề phức tạp, bạn thường:  
**A.** Tách vấn đề thành từng phần, xác định nguyên nhân và mối liên hệ giữa chúng. → `M1 +2`  
**B.** Xem xét những yếu tố nổi bật rồi tìm hướng giải quyết. → `M1 +1`  
**C.** Thử ngay cách đầu tiên mình nghĩ đến. → `M1 +0`

#### **M1.3**

Khi phải chọn giữa nhiều phương án, bạn thường:  
**A.** Đặt ra tiêu chí, so sánh ưu–nhược điểm của từng phương án rồi mới chọn. → `M1 +2`  
**B.** So sánh một vài điểm quan trọng rồi đưa ra lựa chọn. → `M1 +1`  
**C.** Chọn phương án tạo cho mình cảm giác phù hợp nhất lúc đó. → `M1 +0`

#### **M1.5**

Sau khi hoàn thành một bài hoặc nhiệm vụ nhưng kết quả không như mong đợi, bạn thường:  
**A.** Xem lại từng bước để xác định chính xác chỗ sai và nguyên nhân. → `M1 +2`  
**B.** Xem lại những phần mình chưa chắc chắn. → `M1 +1`  
**C.** Làm lại theo một cách khác mà không phân tích nhiều lỗi cũ. → `M1 +0`

### **M2 — Creative Intelligence: Trí thông minh sáng tạo**

#### **M2.1**

Khi cách giải quyết quen thuộc không còn hiệu quả, bạn thường:  
**A.** Thử nhìn vấn đề từ nhiều góc độ và tạo ra một số hướng giải quyết mới. → `M2 +2`  
**B.** Điều chỉnh một vài phần của cách làm cũ. → `M2 +1`  
**C.** Tiếp tục cách cũ hoặc chờ một cách làm mẫu. → `M2 +0`

#### **M2.2**

Khi nhận một nhiệm vụ mới nhưng hướng dẫn chưa đầy đủ, bạn thường:  
**A.** Dựa vào mục tiêu và kinh nghiệm liên quan để chủ động xây dựng cách tiếp cận. → `M2 +2`  
**B.** Thử một cách tương đối quen thuộc rồi điều chỉnh dần. → `M2 +1`  
**C.** Khó bắt đầu cho đến khi có hướng dẫn cụ thể. → `M2 +0`

#### **M2.3**

Khi cần nghĩ ý tưởng cho một sản phẩm hoặc hoạt động, bạn thường:  
**A.** Kết hợp những yếu tố từ nhiều nguồn để tạo thành một hướng mới. → `M2 +2`  
**B.** Phát triển thêm từ một ý tưởng đã có. → `M2 +1`  
**C.** Chọn một mẫu quen thuộc và làm gần giống mẫu đó. → `M2 +0`

#### **M2.5**

Khi một vấn đề có nhiều cách hiểu khác nhau, bạn thường:  
**A.** Khám phá các cách hiểu và dùng chúng để tìm ra khả năng chưa được chú ý. → `M2 +2`  
**B.** Cân nhắc thêm một cách hiểu khác trước khi chọn. → `M2 +1`  
**C.** Giữ cách hiểu đầu tiên để tránh làm vấn đề phức tạp hơn. → `M2 +0`

### **M3 — Practical Intelligence: Trí thông minh thực tiễn**

#### **M3.1**

Khi kế hoạch tốt trên lý thuyết nhưng không phù hợp với thời gian và nguồn lực thực tế, bạn thường:  
**A.** Điều chỉnh kế hoạch theo giới hạn hiện có nhưng vẫn giữ mục tiêu quan trọng nhất. → `M3 +2`  
**B.** Cắt bớt một số phần để kế hoạch có thể thực hiện. → `M3 +1`  
**C.** Cố làm theo kế hoạch ban đầu dù khó hoàn thành. → `M3 +0`

#### **M3.2**

Khi bước vào một môi trường mới có những quy tắc chưa được nói rõ, bạn thường:  
**A.** Quan sát cách mọi người hành động, hỏi khi cần và điều chỉnh cách ứng xử. → `M3 +2`  
**B.** Làm theo những gì mình biết rồi thay đổi nếu được nhắc. → `M3 +1`  
**C.** Giữ nguyên cách làm quen thuộc của mình. → `M3 +0`

#### **M3.3**

Khi có nhiều việc quan trọng nhưng không đủ thời gian làm tất cả, bạn thường:  
**A.** Xác định việc ảnh hưởng lớn và khẩn cấp nhất để ưu tiên nguồn lực. → `M3 +2`  
**B.** Làm lần lượt theo hạn nộp hoặc thứ tự được giao. → `M3 +1`  
**C.** Bắt đầu bằng việc dễ làm nhất. → `M3 +0`

#### **M3.5**

Khi giải pháp đang thực hiện phát sinh một trở ngại bất ngờ, bạn thường:  
**A.** Đánh giá tình hình, tận dụng nguồn lực sẵn có và chuyển sang phương án khả thi hơn. → `M3 +2`  
**B.** Điều chỉnh một phần giải pháp để tiếp tục. → `M3 +1`  
**C.** Tạm dừng và chờ điều kiện trở lại như dự kiến. → `M3 +0`

## **4\. Output cho người dùng**

Ví dụ:

> **M1 — Analytical Intelligence: 7/8 — 88/100**  
> **M2 — Creative Intelligence: 6/8 — 75/100**  
> **M3 — Practical Intelligence: 7/8 — 88/100**

# **Logic tổng hợp kết quả**

Mỗi nhóm có:

> **4 câu × 2 điểm \= tối đa 8 điểm**

### **Bước 1 — Tính điểm 3 nhóm**

> **M1 \= điểm M1 / 8 × 100**

> **M2 \= điểm M2 / 8 × 100**

> **M3 \= điểm M3 / 8 × 100**

Ví dụ:

| Dimension | Score |
| ----- | ----- |
| M1 — Information Processing | 80% |
| M2 — Cognitive Flexibility | 60% |
| M3 — Decision Making | 80% |

# **A — ADAPTABILITY**

### **“Tôi thích nghi với thay đổi như thế nào?”**

### **Dựa vào nền tảng lý thuyết**

**Career Construction Theory** — Savickas (2005) và **Career Adapt-Abilities Scale (CAAS)** — Savickas & Porfeli (2012).  
Career Adaptability được xem là nguồn lực giúp cá nhân **đối phó với nhiệm vụ nghề nghiệp, chuyển tiếp và những thay đổi trong quá trình phát triển nghề nghiệp**.  
CAAS gồm **4 dimensions**:

* **A1 — Concern:** Tôi có quan tâm và chuẩn bị cho tương lai không?  
* **A2 — Control:** Tôi có chủ động và chịu trách nhiệm với hướng đi của mình không?  
* **A3 — Curiosity:** Tôi có khám phá bản thân và những khả năng nghề nghiệp khác nhau không?  
* **A4 — Confidence:** Tôi có tin tưởng vào khả năng vượt qua khó khăn và giải quyết vấn đề của mình không?

> **Lưu ý:** DESMAP sử dụng 4 dimensions này làm **cơ sở lý thuyết để xây dựng nhóm A**, không đồng nghĩa DESMAP là CAAS hay sử dụng nguyên bản CAAS.

# **A1 — CONCERN**

### **Quan tâm & chuẩn bị cho tương lai**

**A1.1** Khi nghĩ về nghề nghiệp trong tương lai, bạn thường:  
A. Chủ động tìm hiểu những khả năng mình có thể theo đuổi. → `A1 +2`  
B. Nghĩ về tương lai nhưng chưa tìm hiểu cụ thể. → `A1 +1`  
C. Chỉ bắt đầu quan tâm khi phải đưa ra lựa chọn. → `A1 +0`  
**A1.2** Nếu ngành nghề bạn đang quan tâm có nhiều thay đổi trong tương lai, bạn thường:  
A. Tìm hiểu trước những thay đổi có thể xảy ra. → `A1 +2`  
B. Theo dõi thêm rồi mới quyết định có cần thay đổi không. → `A1 +1`  
C. Chỉ quan tâm khi sự thay đổi thực sự xảy ra. → `A1 +0`  
**A1.3** Khi lập kế hoạch cho tương lai, bạn thường:  
A. Chuẩn bị nhiều khả năng khác nhau. → `A1 +2`  
B. Có một kế hoạch chính và một vài phương án dự phòng. → `A1 +1`  
C. Tập trung vào kế hoạch hiện tại. → `A1 +0`

# **A2 — CONTROL**

### **Chủ động & chịu trách nhiệm với hướng đi**

**A2.1** Nếu nhận ra lựa chọn nghề nghiệp hiện tại không còn phù hợp với mình, bạn thường:  
A. Chủ động tìm hiểu và điều chỉnh hướng đi. → `A2 +2`  
B. Cân nhắc một thời gian trước khi thay đổi. → `A2 +1`  
C. Tiếp tục lựa chọn hiện tại vì đã bắt đầu rồi. → `A2 +0`  
**A2.2** Khi phải đưa ra một quyết định quan trọng về tương lai, bạn thường:  
A. Tự tìm hiểu và đưa ra quyết định dựa trên thông tin mình có. → `A2 +2`  
B. Tham khảo ý kiến người khác rồi tự quyết định. → `A2 +1`  
C. Chờ người khác định hướng cho mình. → `A2 +0`  
**A2.3** Nếu kế hoạch nghề nghiệp của bạn không diễn ra như dự kiến, bạn thường:  
A. Xác định mình có thể thay đổi điều gì để tiếp tục. → `A2 +2`  
B. Điều chỉnh kế hoạch nếu tình hình không cải thiện. → `A2 +1`  
C. Chờ xem tình hình sẽ tự thay đổi như thế nào. → `A2 +0`

# **A3 — CURIOSITY**

### **Khám phá bản thân & những khả năng mới**

**A3.1** Nếu được giới thiệu một nghề mà trước đây bạn chưa từng nghĩ đến, bạn thường:  
A. Tìm hiểu xem nghề đó có phù hợp với mình không. → `A3 +2`  
B. Nghe thêm thông tin nhưng chưa chắc sẽ tìm hiểu sâu. → `A3 +1`  
C. Bỏ qua vì đó không phải nghề mình đang quan tâm. → `A3 +0`  
**A3.2** Khi phát hiện một lĩnh vực mới có thể phù hợp với năng lực của mình, bạn thường:  
A. Chủ động tìm hiểu và thử trải nghiệm nó. → `A3 +2`  
B. Tìm hiểu thêm trước khi quyết định có thử hay không. → `A3 +1`  
C. Tiếp tục tập trung vào lĩnh vực ban đầu. → `A3 +0`  
**A3.3** Khi tìm hiểu về một nghề, bạn thường muốn biết:  
A. Nhiều khía cạnh khác nhau của nghề, kể cả những điều mình chưa biết. → `A3 +2`  
B. Những thông tin cần thiết để quyết định có phù hợp hay không. → `A3 +1`  
C. Chủ yếu những thông tin xác nhận rằng nghề đó phù hợp với mình. → `A3 +0`

# **A4 — CONFIDENCE**

### **Tin tưởng vào khả năng của bản thân**

**A4.1** Nếu phải học một kỹ năng hoàn toàn mới để theo đuổi một hướng nghề nghiệp, bạn thường:  
A. Tin rằng mình có thể học được nếu dành đủ thời gian và nỗ lực. → `A4 +2`  
B. Thử học trước rồi đánh giá khả năng của mình. → `A4 +1`  
C. Chỉ muốn theo hướng đó nếu mình đã có sẵn kỹ năng cần thiết. → `A4 +0`  
**A4.2** Khi bước vào một môi trường hoàn toàn mới, bạn thường:  
A. Chủ động quan sát, học hỏi và điều chỉnh để hòa nhập. → `A4 +2`  
B. Cần một khoảng thời gian để làm quen rồi mới chủ động hơn. → `A4 +1`  
C. Khó bắt đầu nếu chưa biết rõ mình phải làm gì. → `A4 +0`  
**A4.3** Nếu một lựa chọn nghề nghiệp đòi hỏi bạn phải thay đổi cách làm quen thuộc, bạn thường:  
A. Sẵn sàng thử cách mới và điều chỉnh trong quá trình thực hiện. → `A4 +2`  
B. Thử thay đổi nếu thấy cách cũ không còn hiệu quả. → `A4 +1`  
C. Ưu tiên giữ cách làm mình đã quen. → `A4 +0`

# **Tổng hợp điểm A**

| Dimension | Nội dung | Số câu | Điểm tối đa |
| ----- | ----- | ----- | ----- |
| **A1 — Concern** | Quan tâm & chuẩn bị cho tương lai | 3 | 6 |
| **A2 — Control** | Chủ động & kiểm soát hướng đi | 3 | 6 |
| **A3 — Curiosity** | Khám phá khả năng mới | 3 | 6 |
| **A4 — Confidence** | Tin tưởng vào khả năng bản thân | 3 | 6 |
| **A — Adaptability** | **Tổng** | **12** | **24** |

**Điểm A \= (Tổng điểm đạt được / 24\) × 100**  
AI đồng thời lưu điểm của từng dimension để tạo **Adaptability Profile**, thay vì chỉ đưa ra một điểm tổng.

### **Ví dụ:**

> **Adaptability — 79%**  
> Concern — 83%  
> Control — 67%  
> Curiosity — 84%  
> Confidence — 82%

→ AI có thể diễn giải rằng người dùng **có xu hướng chủ động khám phá và chuẩn bị cho tương lai, khá tự tin khi đối mặt với thay đổi, nhưng mức độ chủ động kiểm soát hướng đi vẫn có thể được cải thiện.**

### **Vai trò của A trong DESMAP**

A bổ sung một khía cạnh mà **hồ sơ tĩnh** như sở thích, năng lực hay tính cách khó phản ánh đầy đủ:

> **“Khi nghề nghiệp và môi trường thay đổi, tôi có khả năng thích nghi với thay đổi đó như thế nào?”**

# **P — PRESSURE**

### **“Tôi phản ứng như thế nào khi công việc trở nên áp lực?”**

### **Mục đích**

P đo **cách một cá nhân phản ứng khi phải đối mặt với các yêu cầu, áp lực và tình huống căng thẳng trong môi trường nghề nghiệp**.  
P trả lời câu hỏi:

> **“Khi công việc trở nên áp lực, tôi phản ứng và duy trì hiệu quả như thế nào?”**

P **không đo mức độ stress hay sức khỏe tâm lý**. P tập trung vào **phản ứng trước các dạng áp lực có thể xuất hiện trong công việc**.  
Cơ sở lý thuyết chính có thể tham khảo:  
**Challenge–Hindrance Stressor Framework** — LePine, Podsakoff & LePine (2005): phân biệt các dạng stressor/challenge trong công việc.

> **Lưu ý:** Các dimension dưới đây là cấu trúc được **xây dựng cho DEPMAP dựa trên các nhóm work demands/stressors trong nghiên cứu**, không phải nguyên bản một thang đo có sẵn.

# **1\. Cấu trúc P**

P gồm **6 dimensions**, mỗi nhóm **2 câu**, tổng cộng **12 câu**.

| Mã | Dimension | Nội dung |
| ----- | ----- | ----- |
| **P1** | Time & Pace Pressure | Áp lực thời gian và tốc độ |
| **P2** | Workload Pressure | Áp lực khối lượng công việc |
| **P3** | Cognitive Pressure | Áp lực tư duy, quyết định |
| **P4** | Emotional Pressure | Áp lực kiểm soát cảm xúc |
| **P5** | Interpersonal Pressure | Áp lực từ tương tác và xung đột |
| **P6** | Responsibility Pressure | Áp lực từ trách nhiệm và hậu quả |

# **2\. Logic chấm điểm**

Mỗi câu có **3 lựa chọn**.

* Phản ứng thể hiện **khả năng duy trì hiệu quả tốt dưới áp lực** → `+2`  
* Phản ứng **trung gian** → `+1`  
* Phản ứng cho thấy **dễ bị ảnh hưởng bởi áp lực** → `+0`

Mỗi dimension có 2 câu:

> **Điểm tối đa mỗi P \= 4**

Quy đổi:

| Điểm | % |
| ----- | ----- |
| 0/4 | 0% |
| 1/4 | 25% |
| 2/4 | 50% |
| 3/4 | 75% |
| 4/4 | 100% |

# **3\. BỘ 12 CÂU**

## **P1 — TIME & PACE PRESSURE**

### **Áp lực thời gian và tốc độ**

### **P1.1**

Bạn đang làm một nhiệm vụ quan trọng nhưng thời gian còn lại chỉ bằng một nửa dự kiến. Bạn sẽ:  
**A.** Xác định phần quan trọng nhất và tập trung hoàn thành trước. → `P1 +2`  
**B.** Tăng tốc độ làm việc và cố gắng hoàn thành toàn bộ. → `P1 +1`  
**C.** Khó tập trung vì quá lo lắng về thời gian. → `P1 +0`

### **P1.2**

Bạn phải hoàn thành nhiều việc trong một khoảng thời gian rất ngắn. Bạn thường:  
**A.** Sắp xếp thứ tự ưu tiên rồi xử lý từng việc. → `P1 +2`  
**B.** Làm nhiều việc cùng lúc để tiết kiệm thời gian. → `P1 +1`  
**C.** Dễ bị rối vì có quá nhiều việc phải hoàn thành. → `P1 +0`

# **P2 — WORKLOAD PRESSURE**

### **Áp lực khối lượng công việc**

### **P2.1**

Bạn được giao nhiều nhiệm vụ cùng lúc và tất cả đều quan trọng. Bạn sẽ:  
**A.** Phân loại mức độ ưu tiên và xây dựng cách xử lý. → `P2 +2`  
**B.** Bắt đầu từ nhiệm vụ dễ hoặc quen thuộc nhất. → `P2 +1`  
**C.** Cảm thấy quá tải vì không biết nên bắt đầu từ đâu. → `P2 +0`

### **P2.2**

Nếu khối lượng công việc tăng đột ngột, bạn thường:  
**A.** Điều chỉnh kế hoạch và phân bổ lại thời gian. → `P2 +2`  
**B.** Cố gắng làm nhiều hơn để theo kịp. → `P2 +1`  
**C.** Dễ mất kiểm soát vì lượng công việc quá lớn. → `P2 +0`

# **P3 — COGNITIVE PRESSURE**

### **Áp lực tư duy và ra quyết định**

### **P3.1**

Bạn phải đưa ra một quyết định quan trọng trong khi chưa có đầy đủ thông tin. Bạn sẽ:  
**A.** Xác định thông tin quan trọng nhất và đưa ra quyết định dựa trên những gì có thể kiểm chứng. → `P3 +2`  
**B.** Cân nhắc các khả năng rồi chọn phương án phù hợp nhất. → `P3 +1`  
**C.** Khó quyết định vì sợ lựa chọn sai. → `P3 +0`

### **P3.3**

Bạn phải đưa ra quyết định nhanh trong một tình huống có rủi ro. Bạn thường:  
**A.** Xác định rủi ro lớn nhất và quyết định dựa trên yếu tố quan trọng nhất. → `P3 +2`  
**B.** Cân nhắc nhanh ưu và nhược điểm trước khi chọn. → `P3 +1`  
**C.** Khó đưa ra quyết định khi không có đủ thời gian suy nghĩ. → `P3 +0`

# **P4 — EMOTIONAL PRESSURE**

### **Áp lực cảm xúc**

### **P4.2**

Bạn vừa mắc một lỗi nghiêm trọng và biết rằng người khác đang chờ kết quả. Bạn sẽ:  
**A.** Tập trung xác định lỗi và tìm cách khắc phục. → `P4 +2`  
**B.** Cảm thấy lo lắng nhưng vẫn cố gắng sửa lỗi. → `P4 +1`  
**C.** Quá lo lắng về hậu quả nên khó tập trung xử lý. → `P4 +0`

### **P4.3**

Bạn nhận được lời phê bình khá gay gắt ngay trước khi tiếp tục công việc. Bạn thường:  
**A.** Tách cảm xúc khỏi vấn đề và tập trung vào điều cần cải thiện. → `P4 +2`  
**B.** Cảm thấy khó chịu nhưng vẫn tiếp tục công việc. → `P4 +1`  
**C.** Bị ảnh hưởng tâm lý và khó tập trung sau đó. → `P4 +0`

# **P5 — INTERPERSONAL PRESSURE**

### **Áp lực từ tương tác và xung đột**

### **P5.1**

Một người liên tục phản đối ý kiến của bạn trong khi công việc đang cần được giải quyết nhanh. Bạn sẽ:  
**A.** Giữ bình tĩnh và tập trung tìm giải pháp cho vấn đề. → `P5 +2`  
**B.** Bảo vệ quan điểm của mình nhưng vẫn cố tiếp tục công việc. → `P5 +1`  
**C.** Cảm thấy căng thẳng và khó tiếp tục trao đổi. → `P5 +0`

### **P5.2**

Một khách hàng/đối tác đang rất không hài lòng với kết quả công việc. Bạn sẽ:  
**A.** Lắng nghe vấn đề và tập trung tìm cách xử lý. → `P5 +2`  
**B.** Giải thích tình hình và cố gắng tìm giải pháp phù hợp. → `P5 +1`  
**C.** Cảm thấy áp lực và khó biết nên phản ứng thế nào. → `P5 +0`

# **P6 — RESPONSIBILITY PRESSURE**

### **Áp lực trách nhiệm và hậu quả**

### **P6.1**

Bạn phải đưa ra một quyết định mà kết quả có thể ảnh hưởng trực tiếp đến người khác. Bạn sẽ:  
**A.** Đánh giá thông tin, hậu quả và đưa ra quyết định có cơ sở. → `P6 +2`  
**B.** Tham khảo ý kiến người khác trước khi quyết định. → `P6 +1`  
**C.** Cảm thấy rất áp lực vì sợ mình đưa ra quyết định sai. → `P6 +0`

### **P6.2**

Bạn là người chịu trách nhiệm chính cho một nhiệm vụ quan trọng. Khi có vấn đề xảy ra:  
**A.** Chủ động nhận trách nhiệm và tập trung giải quyết vấn đề. → `P6 +2`  
**B.** Tìm sự hỗ trợ từ những người liên quan rồi xử lý. → `P6 +1`  
**C.** Dễ hoang mang vì cảm thấy toàn bộ trách nhiệm đang đặt lên mình. → `P6 +0`

**4\. TỔNG HỢP KẾT QUẢ**  
Mỗi dimension:

> **2 câu × 2 điểm \= tối đa 4 điểm**

| Dimension | Nội dung | Số câu | Điểm tối đa |
| ----- | ----- | ----- | ----- |
| **P1 — Time & Pace** | Thời gian & tốc độ | 2 | 4 |
| **P2 — Workload** | Khối lượng công việc | 2 | 4 |
| **P3 — Cognitive** | Tư duy & quyết định | 2 | 4 |
| **P4 — Emotional** | Cảm xúc | 2 | 4 |
| **P5 — Interpersonal** | Tương tác & xung đột | 2 | 4 |
| **P6 — Responsibility** | Trách nhiệm & hậu quả | 2 | 4 |
| **P — Pressure** | **Tổng** | **12** | **24** |

### **Công thức**

> **P1 \= (điểm đạt được / 4\) × 100**

Tương tự cho P2 → P6.

> **Overall Pressure Response \= (tổng điểm / 24\) × 100**

# **5\. OUTPUT CHO NGƯỜI CHƠI**

Ví dụ:

### **PRESSURE PROFILE**

> **Overall Pressure Response — 78%**

| Dimension | Score |
| ----- | ----- |
| Time & Pace | **83%** |
| Workload | **67%** |
| Cognitive | **83%** |
| Emotional | **67%** |
| Interpersonal | **83%** |
| Responsibility | **83%** |

