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
**D1 \- Achievement**

**D1.1 Nếu ba công việc có lương tương đương, bạn dễ chọn công việc nào hơn?**

**A. Công việc quen dần theo thời gian và ít đổi mục tiêu. \-\> \+0**  
**B. Công việc có mục tiêu mới theo từng giai đoạn. \-\> \+1**  
**C. Công việc thường có mục tiêu khó hơn để thử sức. \-\> \+2**

**D1.2 Sau một thời gian làm việc, điều nào làm bạn hài lòng hơn?**

**A. Công việc ổn định và mình làm quen tay hơn. \-\> \+0**  
**B. Mình hoàn thành những mục tiêu đã đặt ra. \-\> \+1**  
**C. Mình làm được việc trước đây chưa từng làm. \-\> \+2**

## **D2 \- Support**

**D2.1 Khi làm một việc mới, bạn thích môi trường nào hơn?**

**A. Được tự tìm cách làm và hỏi khi cần. \-\> \+0**  
**B. Có người hỗ trợ ở một số lúc quan trọng. \-\> \+1**  
**C. Có người hướng dẫn và góp ý thường xuyên. \-\> \+2**

**D2.2 Nếu hai nơi có công việc giống nhau, bạn dễ chọn nơi nào?**

**A. Nơi cho mình nhiều không gian tự xử lý. \-\> \+0**  
**B. Nơi có hỗ trợ khi gặp khó khăn. \-\> \+1**  
**C. Nơi có hướng dẫn đều đặn trong quá trình làm. \-\> \+2**

## **D3 \- Independence**

**D3.1 Khi nhận một nhiệm vụ mới, bạn thích:**

**A. Có cách làm khá rõ để làm theo. \-\> \+0**  
**B. Có khung chung và được chỉnh một số phần. \-\> \+1**  
**C. Biết kết quả cần đạt và tự chọn cách làm. \-\> \+2**

**D3.2 Bạn thích nơi làm việc nào hơn?**

**A. Mọi người dùng cách làm khá giống nhau. \-\> \+0**  
**B. Có thể đổi cách làm sau khi trao đổi. \-\> \+1**  
**C. Mỗi người có cách riêng miễn đạt kết quả. \-\> \+2**

## **D4 \- Relationships**

**D4.1 Trong công việc, bạn thích kiểu tương tác nào hơn?**

**A. Có nhiều thời gian tập trung vào phần việc riêng. \-\> \+0**  
**B. Vừa làm phần việc riêng, vừa trao đổi khi cần. \-\> \+1**  
**C. Thường xuyên làm việc cùng hoặc hỗ trợ người khác. \-\> \+2**

**D4.2 Nếu ba công việc đều phù hợp, bạn dễ chọn công việc nào hơn?**

**A. Công việc tập trung vào kết quả cá nhân của mình. \-\> \+0**  
**B. Công việc vừa có kết quả cá nhân, vừa có tương tác với người khác. \-\> \+1**  
**C. Công việc cho mình thấy rõ tác động đến người khác. \-\> \+2**

## **D5 \- Recognition**

**D5.1 Khi hoàn thành tốt một việc, điều nào khiến bạn vui hơn?**

**A. Bản thân biết mình đã làm tốt. \-\> \+0**  
**B. Người làm cùng biết phần mình đóng góp. \-\> \+1**  
**C. Phần đóng góp của mình được nhắc đến rõ hơn. \-\> \+2**

**D5.2 Khi phát triển lâu dài trong một lĩnh vực, bạn thích hướng nào hơn?**

**A. Trở nên vững chuyên môn trong vai trò hiện tại. \-\> \+0**  
**B. Nhận thêm trách nhiệm khi thấy phù hợp. \-\> \+1**  
**C. Có vai trò ảnh hưởng nhiều hơn đến quyết định chung. \-\> \+2**

## **D6 \- Working Conditions**

**D6.1 Nếu hai công việc đều khiến bạn hứng thú, yếu tố nào dễ ảnh hưởng đến lựa chọn của bạn hơn?**

**A. Nội dung công việc và trải nghiệm khi làm. \-\> \+0**  
**B. Cả nội dung công việc lẫn điều kiện làm việc. \-\> \+1**  
**C. Thu nhập, thời gian và môi trường làm việc ổn định. \-\> \+2**

**D6.2 Một công việc rất thú vị nhưng giờ làm thay đổi nhiều và thu nhập chưa ổn định. Bạn:**

**A. Vẫn dễ chọn vì nội dung công việc hấp dẫn. \-\> \+0**  
**B. Cân nhắc giữa điểm hấp dẫn và điểm chưa ổn định. \-\> \+1**  
**C. Dễ nghiêng về công việc ổn định hơn. \-\> \+2**

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
**E1 \- Basic Skills**

**E1.1 Khi nhận một hướng dẫn dài cho việc mới, bạn thường bắt đầu bằng cách:**

**A. Xem ví dụ hoặc mẫu trước. \-\> \+0**  
**B. Đọc toàn bộ một lượt rồi làm. \-\> \+1**  
**C. Tóm lại mục tiêu và yêu cầu chính trước khi làm. \-\> \+2**

**E1.2 Nhóm cần kiểm tra một bảng chi phí. Bạn thường:**

**A. Dùng công cụ hoặc mẫu có sẵn để kiểm tra. \-\> \+0**  
**B. Tính các khoản chính rồi đối chiếu lại. \-\> \+1**  
**C. Kiểm tra phép tính và các điểm chưa hợp lý. \-\> \+2**

## **E2 \- Complex Problem Solving**

**E2.1 Dự án cùng lúc bị thiếu người, trễ giờ và có lỗi. Bạn thường:**

**A. Làm việc xử lý nhanh nhất trước. \-\> \+0**  
**B. Làm việc đang gấp nhất trước. \-\> \+1**  
**C. Làm việc đang ảnh hưởng nhiều phần khác trước. \-\> \+2**

**E2.2 Có ba cách giải quyết và không cách nào hoàn hảo. Bạn thường:**

**A. Chọn cách mình từng dùng hiệu quả. \-\> \+0**  
**B. So sánh ưu và nhược điểm chính. \-\> \+1**  
**C. So sánh theo vài tiêu chí quan trọng. \-\> \+2**

## **E3 \- Social Skills**

**E3.1 Một người gửi phần việc cho bạn trễ hơn dự kiến. Bạn thường:**

**A. Điều chỉnh phần của mình theo tiến độ mới. \-\> \+0**  
**B. Hỏi lại thời gian cụ thể để sắp xếp tiếp. \-\> \+1**  
**C. Trao đổi cách phối hợp để hạn chế ảnh hưởng đến việc chung. \-\> \+2**

**E3.2 Một vài người không đồng ý với ý tưởng của bạn. Bạn thường:**

**A. Trình bày cách mình nghĩ rồi để nhóm cân nhắc. \-\> \+0**  
**B. Trao đổi thêm về điểm hai bên nghĩ khác nhau. \-\> \+1**  
**C. Tìm điều họ quan tâm để chọn cách trao đổi phù hợp. \-\> \+2**

## **E4 \- Technical Skills**

**E4.1 Bạn cần dùng một công cụ đã lâu không dùng. Bạn thường:**

**A. Xem lại hướng dẫn hoặc ví dụ. \-\> \+0**  
**B. Thử phần mình nhớ và xem lại phần chưa chắc. \-\> \+1**  
**C. Thao tác từng bước và kiểm tra kết quả. \-\> \+2**

**E4.2 Một công cụ quen thuộc đột nhiên hoạt động khác bình thường. Bạn:**

**A. Xem hướng dẫn hoặc tìm lỗi tương tự. \-\> \+0**  
**B. Thử những cách xử lý thường dùng. \-\> \+1**  
**C. Kiểm tra từng phần để tìm lỗi xuất hiện từ đâu. \-\> \+2**

## **E5 \- Systems Skills**

**E5.1 Một phần của dự án bị trễ hai ngày. Bạn thường:**

**A. Tìm cách giúp phần đó xong nhanh hơn. \-\> \+0**  
**B. Sắp xếp lại những việc đang chờ phần đó. \-\> \+1**  
**C. Xem phần nào bị ảnh hưởng rồi chỉnh lại kế hoạch chung. \-\> \+2**

**E5.2 Nhóm muốn dùng cách làm mới nhanh hơn nhưng cần thời gian làm quen. Bạn:**

**A. Thử cách mới ở một phần nhỏ trước. \-\> \+0**  
**B. So sánh thời gian học và thời gian tiết kiệm được. \-\> \+1**  
**C. Xem cách mới ảnh hưởng đến thời gian, chi phí và các phần khác. \-\> \+2**

## **E6 \- Resource Management**

**E6.1 Bạn có ba việc cùng hạn nhưng không đủ thời gian làm kỹ cả ba. Bạn:**

**A. Làm lần lượt theo hạn hoặc thứ tự nhận việc. \-\> \+0**  
**B. Chia thời gian để việc nào cũng có tiến triển. \-\> \+1**  
**C. Chia thời gian theo mức quan trọng và thời gian cần làm. \-\> \+2**

**E6.2 Bạn cần chia việc cho một nhóm. Bạn thường:**

**A. Để mọi người chọn việc họ muốn làm trước. \-\> \+0**  
**B. Chia tương đối đều rồi đổi nếu cần. \-\> \+1**  
**C. Chia theo thời gian và điểm mạnh của từng người. \-\> \+2**

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

# **`S - SOCIAL ROLE`**

**`Khi làm việc với người khác, bạn thường góp phần theo cách nào?`**

## **`S1 - Task Roles`**

### **`Initiator`**

**`S1.1`** `Nhóm đang bắt đầu một dự án mới. Bạn thường:`

`A. Nghe các hướng được đưa ra rồi chọn hướng phù hợp. -> +0`  
`B. Phát triển thêm từ ý tưởng người khác đưa ra. -> +1`  
`C. Đưa ra hướng mới để nhóm cùng xem xét. -> +2`

**`S1.2`** `Cách nhóm đang làm chưa hiệu quả. Bạn thường:`

`A. Điều chỉnh phần mình trước. -> +0`  
`B. Gợi ý đổi một vài bước. -> +1`  
`C. Đề xuất một cách tiếp cận khác. -> +2`

### **`Information Giver`**

**`S1.3`** `Nhóm đang bàn về một chủ đề bạn biết khá rõ. Bạn thường:`

`A. Dùng thông tin đó cho phần việc của mình. -> +0`  
`B. Chia sẻ khi thông tin liên quan trực tiếp. -> +1`  
`C. Bổ sung thông tin để nhóm có thêm dữ liệu. -> +2`

**`S1.4`** `Nhóm chưa đủ thông tin để chọn phương án. Bạn thường:`

`A. Xác định thông tin mình cần cho phần việc. -> +0`  
`B. Cùng người khác tìm thông tin còn thiếu. -> +1`  
`C. Tổng hợp thông tin để nhóm dễ quyết định hơn. -> +2`

### **`Coordinator`**

**`S1.5`** `Một dự án có nhiều người cùng làm. Bạn thường:`

`A. Quản lý phần mình và cập nhật khi cần. -> +0`  
`B. Trao đổi với những người có phần việc liên quan. -> +1`  
`C. Theo dõi cách các phần nối với nhau. -> +2`

**`S1.6`** `Hai phần của dự án đang không khớp nhau. Bạn:`

`A. Điều chỉnh phần mình để dễ ghép lại. -> +0`  
`B. Trao đổi với người phụ trách phần còn lại. -> +1`  
`C. Xem lại cả hai phần và đề xuất cách kết nối. -> +2`

### **`Evaluator-Critic`**

**`S1.7`** `Khi nhóm đưa ra một ý tưởng mới, bạn thường:`

`A. Nghĩ xem ý tưởng có hợp với hướng chung không. -> +0`  
`B. Nhìn vào điểm mạnh và điểm cần chỉnh. -> +1`  
`C. Xem giả định, rủi ro và khả năng thực hiện. -> +2`

**`S1.8`** `Trước khi nộp sản phẩm nhóm, bạn thường:`

`A. Xem tổng thể có đúng hướng không. -> +0`  
`B. Rà yêu cầu và lỗi dễ thấy. -> +1`  
`C. Kiểm tra logic và các điểm chưa thống nhất. -> +2`

## **`S2 - Maintenance / Social Roles`**

### **`Encourager`**

**`S2.1`** `Khi nhóm bắt đầu xuống tinh thần, bạn thường:`

`A. Giữ nhịp phần việc của mình. -> +0`  
`B. Nói chuyện với những người mình làm cùng. -> +1`  
`C. Tìm cách giúp nhóm lấy lại nhịp làm việc. -> +2`

**`S2.2`** `Một thành viên vừa làm tốt một việc. Bạn thường:`

`A. Tiếp tục phối hợp tốt với họ. -> +0`  
`B. Nói riêng rằng phần đó làm tốt. -> +1`  
`C. Nhắc đến đóng góp đó trước nhóm khi phù hợp. -> +2`

### **`Harmonizer`**

**`S2.3`** `Hai người trong nhóm có quan điểm trái nhau. Bạn thường:`

`A. Để hai bên trao đổi trực tiếp. -> +0`  
`B. Tham gia nếu việc chung bị ảnh hưởng. -> +1`  
`C. Giúp hai bên làm rõ điểm khác nhau. -> +2`

**`S2.4`** `Cuộc thảo luận bắt đầu căng thẳng. Bạn thường:`

`A. Cho mọi người thêm thời gian tự điều chỉnh. -> +0`  
`B. Gợi ý quay lại vấn đề cần giải quyết. -> +1`  
`C. Giúp làm rõ ý từng bên để cuộc nói chuyện tiếp tục được. -> +2`

### **`Gatekeeper`**

**`S2.5`** `Một thành viên ít nói trong cuộc họp. Bạn thường:`

`A. Tôn trọng việc họ tự chọn lúc phát biểu. -> +0`  
`B. Tạo khoảng trống để họ tham gia nếu muốn. -> +1`  
`C. Hỏi ý kiến họ khi thấy phù hợp. -> +2`

**`S2.6`** `Một người đang nói nhiều hơn những người khác. Bạn thường:`

`A. Để cuộc trao đổi diễn ra nếu vẫn có ích. -> +0`  
`B. Chuyển câu hỏi sang người khác khi có cơ hội. -> +1`  
`C. Điều tiết để nhiều người có thời gian nói hơn. -> +2`

## **`S3 - Individual-Oriented Roles`**

### **`Dominator`**

**`S3.1`** `Nhóm chưa thể thống nhất hướng làm. Bạn thường:`

`A. Tìm tiếp một hướng nhiều người có thể đồng ý. -> +0`  
`B. Bảo vệ phương án mình thấy hợp lý. -> +1`  
`C. Đứng ra chốt một hướng để nhóm đi tiếp. -> +2`

**`S3.2`** `Bạn tin phương án mình tốt hơn phương án nhóm đang nghiêng về. Bạn:`

`A. Trình bày quan điểm rồi theo lựa chọn chung. -> +0`  
`B. Tiếp tục thuyết phục nếu khác biệt quan trọng. -> +1`  
`C. Tác động để phương án cuối gần với hướng mình đề xuất. -> +2`

### **`Recognition Seeker`**

**`S3.3`** `Khi đóng góp nhiều cho một dự án, bạn thích:`

`A. Kết quả chung tốt là đủ. -> +0`  
`B. Người làm cùng biết phần mình đóng góp. -> +1`  
`C. Vai trò của mình được thể hiện rõ. -> +2`

**`S3.4`** `Ý tưởng của bạn được dùng nhưng mọi người không biết nó bắt đầu từ bạn. Bạn:`

`A. Không cần làm rõ nếu kết quả chung tốt. -> +0`  
`B. Muốn những người làm gần mình biết. -> +1`  
`C. Muốn vai trò của mình trong ý tưởng đó được ghi nhận rõ. -> +2`

### **`Independent Contributor`**

**`S3.5`** `Với một phần việc lớn, bạn thích:`

`A. Làm cùng mọi người khá thường xuyên. -> +0`  
`B. Có lúc làm riêng, có lúc trao đổi cùng nhóm. -> +1`  
`C. Có một phần riêng để tự theo đến cuối. -> +2`

**`S3.6`** `Nếu ba nhiệm vụ quan trọng như nhau, bạn dễ chọn:`

`A. Nhiệm vụ cần phối hợp liên tục. -> +0`  
`B. Nhiệm vụ có cả phần làm chung và làm riêng. -> +1`  
`C. Nhiệm vụ cho mình tự xử lý phần lớn công việc. -> +2`

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

# **`M - MIND`**

**`Bạn thường suy nghĩ theo cách nào khi giải quyết vấn đề?`**

## **`M1 - Analytical`**

**`M1.1`** `Hai nguồn đưa ra hai kết luận khác nhau. Bạn thường:`

`A. Xem nguồn nào quen thuộc và hợp với điều mình biết. -> +0`  
`B. So sánh những điểm chính của hai nguồn. -> +1`  
`C. So sánh bằng chứng và cách lập luận của hai nguồn. -> +2`

**`M1.2`** `Khi gặp một vấn đề có nhiều phần, bạn thường:`

`A. Bắt đầu từ phần dễ thấy nhất. -> +0`  
`B. Xác định vài yếu tố chính cần xử lý. -> +1`  
`C. Tách vấn đề thành các phần và xem chúng liên quan thế nào. -> +2`

**`M1.3`** `Có nhiều phương án khá ngang nhau. Bạn:`

`A. Chọn phương án hợp với tình hình hiện tại. -> +0`  
`B. So sánh những điểm quan trọng nhất. -> +1`  
`C. Đặt tiêu chí rồi đánh giá từng phương án. -> +2`

**`M1.4`** `Kết quả một việc không như mong đợi. Bạn:`

`A. Dùng kinh nghiệm đó để thử cách khác lần sau. -> +0`  
`B. Xem lại những phần mình chưa chắc. -> +1`  
`C. Xem lại từng bước để tìm điểm bắt đầu lệch. -> +2`

## **`M2 - Creative`**

**`M2.1`** `Một cách làm quen thuộc không còn hiệu quả. Bạn:`

`A. Tìm cách khác từng hiệu quả ở tình huống tương tự. -> +0`  
`B. Thay đổi một số phần của cách cũ. -> +1`  
`C. Thử nhìn vấn đề theo hướng khác để tạo cách mới. -> +2`

**`M2.2`** `Bạn nhận một nhiệm vụ mới nhưng hướng dẫn còn mở. Bạn:`

`A. Tìm một ví dụ gần giống để bắt đầu. -> +0`  
`B. Dùng cách quen thuộc rồi chỉnh dần. -> +1`  
`C. Tạo cách làm dựa trên mục tiêu cần đạt. -> +2`

**`M2.3`** `Khi cần nghĩ ý tưởng, bạn thường:`

`A. Bắt đầu từ một mẫu hoặc ý tưởng đã có. -> +0`  
`B. Phát triển ý tưởng cũ theo hướng mới. -> +1`  
`C. Ghép nhiều yếu tố khác nhau để tạo hướng mới. -> +2`

**`M2.4`** `Một vấn đề có thể được hiểu theo nhiều cách. Bạn:`

`A. Chọn cách hiểu hợp với tình huống hiện tại. -> +0`  
`B. Xem thêm vài cách hiểu trước khi chọn. -> +1`  
`C. Nhìn từ nhiều góc để tìm khả năng mới. -> +2`

## **`M3 - Practical`**

**`M3.1`** `Kế hoạch tốt nhưng thực tế không đủ thời gian. Bạn:`

`A. Giữ những phần quan trọng nhất. -> +1`  
`B. Tìm thêm nguồn lực để giữ kế hoạch ban đầu. -> +0`  
`C. Chỉnh cách làm theo điều kiện hiện tại. -> +2`

**`M3.2`** `Bạn bước vào nơi mới và chưa rõ cách mọi người làm việc. Bạn:`

`A. Dùng cách mình quen rồi đổi khi cần. -> +0`  
`B. Hỏi một vài người về cách làm việc ở đó. -> +1`  
`C. Quan sát cách mọi người làm rồi điều chỉnh theo. -> +2`

**`M3.3`** `Bạn không đủ thời gian làm mọi việc như dự kiến. Bạn:`

`A. Làm theo thứ tự đã lên từ đầu. -> +0`  
`B. Điều chỉnh thứ tự theo deadline. -> +1`  
`C. Đổi thứ tự theo mức ảnh hưởng đến kết quả. -> +2`

**`M3.4`** `Cách bạn đang làm gặp trở ngại bất ngờ. Bạn:`

`A. Giữ cách cũ và chỉnh một vài bước. -> +0`  
`B. Chuyển sang phương án dự phòng. -> +1`  
`C. Chọn cách xử lý mới theo tình hình hiện tại. -> +2`

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

# **`A - ADAPTABILITY`**

**`Bạn thường thích nghi với thay đổi trong hướng nghề nghiệp như thế nào?`**

## **`A1 - Concern`**

**`A1.1`** `Khi nghĩ về nghề tương lai, bạn thường:`

`A. Tập trung vào những lựa chọn trước mắt. -> +0`  
`B. Nghĩ đến vài năm tới khi cần chọn hướng. -> +1`  
`C. Tìm hiểu sớm các hướng mình có thể đi. -> +2`

**`A1.2`** `Nghề bạn quan tâm có thể thay đổi nhiều trong vài năm tới. Bạn:`

`A. Tập trung học những gì đang cần hiện tại. -> +0`  
`B. Theo dõi thay đổi khi chúng rõ hơn. -> +1`  
`C. Tìm hiểu sớm nghề đó có thể thay đổi ra sao. -> +2`

**`A1.3`** `Khi nghĩ về tương lai, bạn thường:`

`A. Tập trung vào một hướng chính. -> +0`  
`B. Có một hướng chính và một lựa chọn khác. -> +1`  
`C. Nghĩ trước vài hướng để có thể chuyển nếu cần. -> +2`

## **`A2 - Control`**

**`A2.1`** `Bạn bắt đầu thấy hướng nghề hiện tại không còn hợp mình. Bạn:`

`A. Tiếp tục thêm một thời gian để quan sát cảm giác đó. -> +0`  
`B. Tìm hiểu thêm trước khi quyết định có đổi không. -> +1`  
`C. Xem lại lựa chọn và điều chỉnh hướng đi nếu cần. -> +2`

**`A2.2`** `Khi phải đưa ra lựa chọn nghề quan trọng, bạn:`

`A. Dựa nhiều vào lời khuyên của người hiểu mình. -> +0`  
`B. Kết hợp lời khuyên với suy nghĩ của bản thân. -> +1`  
`C. Tự thu thập thông tin và chọn phương án cuối. -> +2`

**`A2.3`** `Kế hoạch nghề nghiệp không diễn ra như dự kiến. Bạn:`

`A. Cho kế hoạch thêm thời gian trước khi đổi. -> +0`  
`B. Điều chỉnh khi thấy vấn đề kéo dài. -> +1`  
`C. Xem phần nào có thể thay đổi ngay. -> +2`

## **`A3 - Curiosity`**

**`A3.1`** `Có người giới thiệu một nghề bạn chưa từng nghĩ tới. Bạn:`

`A. Ghi nhận nhưng tiếp tục tìm hiểu nghề đang quan tâm. -> +0`  
`B. Tìm hiểu sơ để biết nghề đó làm gì. -> +1`  
`C. Tìm hiểu xem nghề đó có điểm nào hợp với mình. -> +2`

**`A3.2`** `Bạn phát hiện một lĩnh vực mới khá thú vị. Bạn:`

`A. Tiếp tục ưu tiên lĩnh vực mình đã chọn. -> +0`  
`B. Đọc thêm để hiểu lĩnh vực mới. -> +1`  
`C. Thử một hoạt động liên quan để biết mình có hợp không. -> +2`

**`A3.3`** `Khi tìm hiểu một nghề, bạn thường:`

`A. Tập trung vào thông tin cần cho quyết định hiện tại. -> +0`  
`B. Xem thêm một số mặt khác của nghề. -> +1`  
`C. Tìm cả những mặt mình chưa biết trước đó. -> +2`

## **`A4 - Confidence`**

**`A4.1`** `Một hướng nghề yêu cầu kỹ năng bạn chưa có. Bạn:`

`A. Xem mình đã có kỹ năng nào gần với yêu cầu đó chưa. -> +0`  
`B. Thử học một thời gian rồi đánh giá lại. -> +1`  
`C. Lên cách học từng bước nếu muốn theo hướng này. -> +2`

**`A4.2`** `Khi bước vào một môi trường rất mới, bạn:`

`A. Tìm hiểu kỹ yêu cầu trước khi bắt đầu. -> +0`  
`B. Làm quen từng bước rồi tham gia nhiều hơn. -> +1`  
`C. Vừa quan sát vừa thử và điều chỉnh trong quá trình. -> +2`

**`A4.3`** `Một công việc buộc bạn đổi cách làm đã quen. Bạn:`

`A. Giữ những phần của cách cũ vẫn còn dùng được. -> +0`  
`B. Thử thay đổi từng phần. -> +1`  
`C. Học cách mới và điều chỉnh khi thực hiện. -> +2`

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

## **`P1 - Time & Pace`**

**`P1.1`** `Thời gian còn lại ít hơn nhiều so với dự kiến. Bạn:`

`A. Giữ kế hoạch chính và tăng tốc độ làm. -> +0`  
`B. Bỏ bớt một số phần ít quan trọng. -> +1`  
`C. Xếp lại việc theo mức quan trọng. -> +2`

**`P1.2`** `Nhiều việc cần hoàn thành trong thời gian ngắn. Bạn:`

`A. Chuyển qua lại giữa các việc để tất cả cùng tiến triển. -> +0`  
`B. Chia thời gian thành từng khoảng cho mỗi việc. -> +1`  
`C. Xác định thứ tự ưu tiên rồi làm lần lượt. -> +2`

## **`P2 - Workload`**

**`P2.1`** `Bạn bất ngờ nhận thêm nhiều việc. Bạn:`

`A. Kéo dài thời gian làm để xử lý thêm. -> +0`  
`B. Chia nhỏ các việc để dễ theo dõi. -> +1`  
`C. Xem lại khối lượng rồi đổi thứ tự và thời gian làm. -> +2`

**`P2.2`** `Có nhiều việc đều được xem là quan trọng. Bạn:`

`A. Bắt đầu từ những việc mình làm nhanh nhất. -> +0`  
`B. Chia thời gian tương đối đều cho các việc. -> +1`  
`C. Chia thời gian theo mức ảnh hưởng của từng việc. -> +2`

## **`P3 - Cognitive Pressure`**

**`P3.1`** `Bạn phải quyết định nhưng thông tin vẫn còn thiếu. Bạn:`

`A. Chọn phương án quen thuộc và ít rủi ro. -> +0`  
`B. Dựa vào những thông tin chính đang có. -> +1`  
`C. Xác định thông tin cần nhất rồi chọn theo dữ kiện đó. -> +2`

**`P3.2`** `Bạn phải quyết định nhanh trong tình huống có rủi ro. Bạn:`

`A. Dựa vào kinh nghiệm từ tình huống gần giống. -> +0`  
`B. So nhanh các lựa chọn chính rồi chọn. -> +1`  
`C. Xem hậu quả quan trọng nhất cần tránh rồi chọn. -> +2`

## **`P4 - Emotional Pressure`**

**`P4.1`** `Bạn vừa mắc lỗi trong một việc quan trọng. Bạn thường:`

`A. Nói với người liên quan để cùng nhìn lại tình huống. -> +0`  
`B. Xác định lỗi chính rồi bắt đầu sửa. -> +1`  
`C. Sắp xếp lại việc cần xử lý theo mức độ ảnh hưởng. -> +2`

**`P4.2`** `Bạn nhận góp ý khá nặng khi đang làm việc. Bạn thường:`

`A. Tạm để đó và quay lại khi bình tĩnh hơn. -> +0`  
`B. Tiếp tục làm rồi xem lại góp ý sau. -> +1`  
`C. Lọc phần góp ý có ích để chỉnh việc đang làm. -> +2`

## **`P5 - Interpersonal Pressure`**

**`P5.1`** `Một người phản đối bạn khá mạnh khi công việc đang gấp. Bạn:`

`A. Tạm gác điểm chưa thống nhất để việc tiếp tục. -> +0`  
`B. Nói ngắn gọn lý do của mình rồi cùng chốt hướng. -> +1`  
`C. Xác định điểm khác nhau và xử lý đúng điểm đó. -> +2`

**`P5.2`** `Khách hàng hoặc đối tác đang rất không hài lòng. Bạn:`

`A. Mời thêm người liên quan cùng trao đổi. -> +0`  
`B. Giải thích tình hình và đề xuất hướng xử lý. -> +1`  
`C. Làm rõ điều họ không hài lòng nhất rồi xử lý trước. -> +2`

## **`P6 - Responsibility Pressure`**

**`P6.1`** `Bạn phải đưa ra quyết định có thể ảnh hưởng đến người khác. Bạn:`

`A. Trao đổi để nhiều người cùng tham gia quyết định. -> +0`  
`B. Xin thêm ý kiến rồi chọn phương án cuối. -> +1`  
`C. Xem các hậu quả có thể xảy ra rồi tự quyết định. -> +2`

**`P6.2`** `Bạn là người phụ trách chính và dự án gặp vấn đề. Bạn:`

`A. Chia vấn đề cho người có chuyên môn phù hợp. -> +0`  
`B. Cùng người liên quan thống nhất cách xử lý. -> +1`  
`C. Xác định vấn đề chính, phân công và theo dõi tiến độ. -> +2`

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

