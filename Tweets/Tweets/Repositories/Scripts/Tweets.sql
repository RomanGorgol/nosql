use master
go

create database Tweets
go

use Tweets
go

create table messages
(
	id UniqueIdentifier primary key not null,
	userName varchar(100),
	[text] varchar(1000),
	createDate datetime,
	[version] RowVersion not null
)
go
create table likes
(
	userName varchar(100) not null,
	messageId UniqueIdentifier foreign key references messages on delete cascade not null,
	createDate datetime
)
alter table likes add constraint PK_Likes primary key(userName,messageId)
go


