import { authenticatedFetch } from '@/auth/auth-api'
const URL = '/api/v1/members'
export const memberStatuses = ['Active','Expired','Suspended','Discontinued'] as const
export type MemberStatus = typeof memberStatuses[number]
export const statusLabels: Record<MemberStatus,string> = {Active:'Đang hoạt động',Expired:'Hết hạn',Suspended:'Tạm đình chỉ',Discontinued:'Ngừng sử dụng'}
export type CardStatus='Active'|'Expired'|'Suspended'|'Revoked'
export type RestrictionType='Borrowing'|'Reservation'|'AllTransactions'
export type Card={id:string;cardNumber:string;issuedOn:string;expiresOn:string;status:CardStatus}
export type Restriction={id:string;type:RestrictionType;reason:string;startsAtUtc:string;endsAtUtc:string|null;removedAtUtc:string|null;removalReason:string|null;isActive:boolean}
export type BorrowingHistory={id:string;bookId:string;borrowedAtUtc:string;dueAtUtc:string;returnedAtUtc:string|null}
export type ReservationHistory={id:string;bookId:string;reservedAtUtc:string;expiresAtUtc:string;fulfilledAtUtc:string|null;cancelledAtUtc:string|null}
export type Fine={id:string;type:string;bookTitle:string;note:string;originalAmount:number;adjustmentTotal:number;paymentTotal:number;balance:number;recordedAtUtc:string;status:string}
export type Member={id:string;memberCode:string;fullName:string;email:string;phoneNumber:string|null;dateOfBirth:string|null;address:string|null;memberGroup:string;status:MemberStatus;borrowingLimit:number;loanPeriodDays:number;concurrencyToken:string;createdAtUtc:string;updatedAtUtc:string;card:Card|null;restrictions:Restriction[];borrowings:BorrowingHistory[];reservations:ReservationHistory[];fines:Fine[]}
export type MemberPageResponse={items:Member[];pageNumber:number;pageSize:number;totalCount:number;totalPages:number}
export type SaveMemberInput=Pick<Member,'memberCode'|'fullName'|'email'|'phoneNumber'|'dateOfBirth'|'address'|'memberGroup'|'status'|'borrowingLimit'|'loanPeriodDays'>&{concurrencyToken?:string}
async function read<T>(r:Response):Promise<T>{if(r.ok)return r.json() as Promise<T>;const p=await r.json().catch(()=>null) as {detail?:string;title?:string;errors?:Record<string,string[]>}|null;throw new Error(p?.errors?Object.values(p.errors).flat()[0]:p?.detail??p?.title??'Không thể xử lý yêu cầu.')}
async function send(path:string,method:string,body:unknown){return read<Member>(await authenticatedFetch(`${URL}${path}`,{method,headers:{'Content-Type':'application/json'},body:JSON.stringify(body)}))}
export async function getMembers(filters:{search?:string;status?:MemberStatus;memberGroup?:string;pageNumber:number;pageSize:number},signal?:AbortSignal){const q=new URLSearchParams();Object.entries(filters).forEach(([k,v])=>{if(v!==undefined&&v!=='')q.set(k,String(v))});return read<MemberPageResponse>(await authenticatedFetch(`${URL}?${q}`,{signal}))}
export async function getMember(id:string,signal?:AbortSignal){return read<Member>(await authenticatedFetch(`${URL}/${id}`,{signal}))}
export const createMember=(body:SaveMemberInput)=>send('','POST',body)
export const updateMember=(id:string,body:SaveMemberInput)=>send(`/${id}`,'PUT',body)
export const issueCard=(id:string,body:{cardNumber:string;issuedOn:string;expiresOn:string})=>send(`/${id}/card`,'POST',{...body,issuedOn:body.issuedOn||new Date().toISOString().slice(0,10)})
export const renewCard=(id:string,expiresOn:string)=>send(`/${id}/card/renew`,'POST',{expiresOn})
export const changeCardStatus=(id:string,status:CardStatus)=>send(`/${id}/card/status`,'PATCH',{status})
export const addRestriction=(id:string,body:{type:RestrictionType;reason:string;startsAtUtc:string;endsAtUtc:string|null})=>send(`/${id}/restrictions`,'POST',body)
export const removeRestriction=(id:string,rid:string,reason:string)=>send(`/${id}/restrictions/${rid}/remove`,'POST',{reason})
export const addPayment=(id:string,vid:string,body:{amount:number;method:'Cash'|'BankTransfer'|'Card'|'Other';reference:string|null})=>send(`/${id}/violations/${vid}/payments`,'POST',body)
export const addAdjustment=(id:string,vid:string,body:{amountDelta:number;reason:string})=>send(`/${id}/violations/${vid}/adjustments`,'POST',body)
