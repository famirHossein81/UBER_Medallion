import streamlit as st
import requests
import pandas as pd
import plotly.express as px
import chromadb
from sentence_transformers import SentenceTransformer

@st.cache_resource
def load_ai_resources():
    print("⏳ Loading AI Models... (This happens only once)")
    chroma_client = chromadb.PersistentClient(path="./chroma_db")
    collection = chroma_client.get_collection(name="cancellation_reasons")
    embedding_model = SentenceTransformer('all-MiniLM-L6-v2')
    return collection, embedding_model

collection, embedding_model = load_ai_resources()

API_BASE_URL = "http://localhost:5023/api" 
st.set_page_config(page_title="Uber Smart Platform", layout="wide")

st.title("🚖 Uber Analytics & AI Manager")

st.sidebar.header("Filter Data")
vehicle_options = ["All", "Auto", "Uber XL", "Go Premier", "Moto", "Go Sedan", "Bike", "eBike", "Go mini"]
selected_vehicle = st.sidebar.selectbox("Select Vehicle Type", vehicle_options)

start_date = st.sidebar.date_input("Start Date", value=None)
end_date = st.sidebar.date_input("End Date", value=None)

params = {}
if selected_vehicle != "All":
    params['vehicleType'] = selected_vehicle
if start_date:
    params['start'] = start_date
if end_date:
    params['end'] = end_date

tab1, tab2, tab3, tab4 = st.tabs(["📊 Dashboard", "🤖 AI Assistant", "🔍 Semantic Search", "🛠️ Trip Manager"])

with tab1:
    st.subheader("Real-time KPIs")
    
    try:
        response = requests.get(f"{API_BASE_URL}/Analytics/kpis", params=params)
        if response.status_code == 200:
            kpi = response.json()
            c1, c2, c3, c4 = st.columns(4)
            c1.metric("Total Bookings", f"{kpi.get('totalBookings', 0):,}")
            c2.metric("Successful", f"{kpi.get('successfulBookings', 0):,}")
            revenue = kpi.get('totalRevenue', 0)
            c3.metric("Total Revenue", f"${revenue:,.2f}")
            rate = kpi.get('successRate', 0)
            c4.metric("Success Rate", f"{rate}%")
        else:
            st.error(f"Error fetching KPIs: {response.status_code}")
    except Exception as e:
        st.error(f"Connection Error: {e}")
        st.info("Is your .NET Web API running?")

    st.divider()

    
    col_r1_1, col_r1_2 = st.columns(2)

    with col_r1_1:
        st.subheader("Vehicle Performance")
        try:
            res = requests.get(f"{API_BASE_URL}/Analytics/charts/vehicles", params=params)
            if res.status_code == 200:
                df_v = pd.DataFrame(res.json())
                if not df_v.empty:
                    
                    fig_v = px.bar(df_v, x='vehicleType', y='tripCount', 
                                   color='averageRating', title="Trips & Ratings per Vehicle")
                    st.plotly_chart(fig_v, use_container_width=True)
                else:
                    st.warning("No data.")
        except:
            st.write("Loading...")

    with col_r1_2:
        st.subheader("Cancellation Reasons")
        try:
            res = requests.get(f"{API_BASE_URL}/Analytics/charts/cancellations", params=params)
            if res.status_code == 200:
                df_c = pd.DataFrame(res.json())
                if not df_c.empty:
                    fig_c = px.pie(df_c, names='label', values='value', hole=0.4, title="Why trips fail?")
                    st.plotly_chart(fig_c, use_container_width=True)
                else:
                    st.write("No cancellations found.")
        except:
            st.write("Loading...")

    st.divider()

    col_r2_1, col_r2_2 = st.columns(2)

    with col_r2_1:
        st.subheader("Payment Methods") 
        try:
            res_pay = requests.get(f"{API_BASE_URL}/Analytics/charts/payment-methods", params=params)
            if res_pay.status_code == 200:
                df_p = pd.DataFrame(res_pay.json())
                if not df_p.empty:
                    fig_p = px.pie(df_p, names='label', values='value', hole=0.4, title="Payment Distribution")
                    st.plotly_chart(fig_p, use_container_width=True)
                else:
                    st.info("No payment data available.")
        except Exception as e:
            st.error(f"Error: {e}")

    with col_r2_2:
        st.subheader("Hourly Traffic Trends")
        try:
            res_traffic = requests.get(f"{API_BASE_URL}/Analytics/charts/traffic-hourly", params=params)
            if res_traffic.status_code == 200:
                df_traffic = pd.DataFrame(res_traffic.json())
                if not df_traffic.empty:
                    fig_traffic = px.line(df_traffic, x='label', y='value', markers=True,
                                          title="Busiest Hours",
                                          labels={'label': 'Hour', 'value': 'Trips'})
                    st.plotly_chart(fig_traffic, use_container_width=True)
        except:
            pass

with tab2:
    st.header("Ask your Database 🧠")
    st.markdown("This Assistant converts your English questions into SQL.")
    user_query = st.text_input("Enter your question:", placeholder="e.g. What is the average revenue per km?")

    if st.button("Ask AI"):
        if not user_query:
            st.warning("Please type a question.")
        else:
            with st.spinner("Thinking..."):
                try:
                    payload = {"question": user_query}
                    headers = {"Content-Type": "application/json"}
                    res = requests.post(f"{API_BASE_URL}/Chat/ask", json=payload, headers=headers)
                    if res.status_code == 200:
                        data = res.json()
                        st.subheader("1. Generated SQL")
                        st.code(data.get('generatedSql'), language='sql')
                        st.subheader("2. Answer")
                        result_data = data.get('answer')
                        if isinstance(result_data, list) and len(result_data) > 0:
                            st.dataframe(pd.DataFrame(result_data), use_container_width=True)
                        else:
                            st.write(result_data)
                    else:
                        st.error(f"API Error: {res.text}")
                except Exception as e:
                    st.error(f"Failed to connect: {e}")

with tab3:
    st.header("Find Similar Cancellations")
    search_query = st.text_input("Describe the issue:", placeholder="e.g. The driver was rude")
    
    if st.button("Search Vectors"):
        if search_query:
            with st.spinner("Searching Vector Space..."):
                query_vec = embedding_model.encode([search_query]).tolist()
                results = collection.query(query_embeddings=query_vec, n_results=5)
                
                if results['ids']:
                    found_ids = results['ids'][0]
                    found_reasons = results['documents'][0]
                    st.success(f"Found {len(found_ids)} relevant trips!")
                    
                    for i, bid in enumerate(found_ids):
                        reason = found_reasons[i]
                        try:
                            api_res = requests.get(f"{API_BASE_URL}/Trips/{bid}")
                            with st.expander(f"Trip {bid[:8]}... - {reason}"):
                                if api_res.status_code == 200:
                                    trip = api_res.json()
                                    c1, c2, c3 = st.columns(3)
                                    c1.write(f"**Status:** {trip.get('bookingStatus')}")
                                    c2.write(f"**Vehicle:** {trip.get('vehicleType')}")
                                    c3.write(f"**Value:** ${trip.get('bookingValue')}")
                                    st.caption(f"Full Reason: {reason}")
                                else:
                                    st.warning(f"Could not fetch details (ID: {bid})")
                        except:
                            st.error("API Connection Failed")
                else:
                    st.warning("No matches found.")

with tab4:
    st.header("🛠️ Manage Trips (CRUD)")

    with st.expander("➕ Create New Trip", expanded=False):
        c1, c2 = st.columns(2)
        with c1:
            new_cust_id = st.text_input("Customer ID", value="CID123456")
            new_vehicle = st.selectbox("Vehicle Type", ["UberXL", "Auto", "Go Sedan", "Premier", "Moto"])
            new_payment = st.selectbox("Payment Method", ["Cash", "UPI", "Credit Card", "Wallet"])
        with c2:
            new_dist = st.number_input("Distance (km)", min_value=0.1, value=5.5)
            new_value = st.number_input("Trip Cost ($)", min_value=1.0, value=25.0)
            new_rating = st.slider("Customer Rating", 1.0, 5.0, 5.0)

        if st.button("Submit New Trip"):
            payload = {
                "customerId": new_cust_id, "vehicleType": new_vehicle,
                "paymentMethod": new_payment, "bookingValue": new_value,
                "rideDistance": new_dist, "customerRating": new_rating
            }
            try:
                res = requests.post(f"{API_BASE_URL}/Trips", json=payload)
                if res.status_code == 201:
                    st.success(f"✅ Trip Created! ID: {res.json().get('bookingId')}")
                else:
                    st.error(f"Failed: {res.text}")
            except Exception as e:
                st.error(f"Connection Error: {e}")

    st.divider()

    col_up, col_del = st.columns(2)

    with col_up:
        st.subheader("✏️ Update Status")
        up_id = st.text_input("Booking ID to Update", placeholder="e.g. CNR123...")
        new_status = st.selectbox("New Status", ["Completed", "Cancelled by Customer", "Cancelled by Driver", "No Driver Found"])
        
        if st.button("Update Status"):
            if up_id:
                try:
                    res = requests.put(f"{API_BASE_URL}/Trips/{up_id}", json={"status": new_status})
                    if res.status_code == 200:
                        st.success("✅ Status Updated!")
                    elif res.status_code == 404:
                        st.error("❌ Trip ID not found.")
                    else:
                        st.error(f"Error: {res.text}")
                except Exception as e:
                    st.error(f"Error: {e}")
            else:
                st.warning("Enter an ID.")

    with col_del:
        st.subheader("🗑️ Delete Trip")
        del_id = st.text_input("Booking ID to Delete", placeholder="e.g. CNR987...")
        
        if st.button("Delete Trip", type="primary"):
            if del_id:
                try:
                    res = requests.delete(f"{API_BASE_URL}/Trips/{del_id}")
                    if res.status_code == 204:
                        st.success("✅ Trip deleted successfully.")
                    elif res.status_code == 404:
                        st.error("❌ Trip ID not found.")
                    else:
                        st.error(f"Error: {res.text}")
                except Exception as e:
                    st.error(f"Error: {e}")
            else:
                st.warning("Enter an ID.")

    st.divider()
    
    st.divider()

    st.subheader("🔍 Find Specific Trip")
    
    c_search, c_btn = st.columns([4, 1])
    
    with c_search:
        lookup_id = st.text_input("Enter Booking ID:", placeholder="e.g. BID-2024-8899")
        
    with c_btn:
        st.write("") 
        st.write("") 
        search_clicked = st.button("Find Trip", use_container_width=True)

    if search_clicked and lookup_id:
        with st.spinner(f"Searching for {lookup_id}..."):
            try:
                res = requests.get(f"{API_BASE_URL}/Trips/{lookup_id}")
                
                if res.status_code == 200:
                    trip = res.json()
                    
                    with st.container(border=True):
                        st.success(f"✅ Found Trip: {trip.get('bookingId')}")
                        
                        col1, col2, col3, col4 = st.columns(4)
                        col1.metric("Status", trip.get('bookingStatus'))
                        col1.caption(f"Date: {trip.get('bookingDate', '').split('T')[0]}")
                        
                        col2.metric("Customer", trip.get('customerId'))
                        col2.caption(f"Rating: {trip.get('customerRating')} ⭐")
                        
                        col3.metric("Vehicle", trip.get('vehicleType'))
                        col3.caption(f"Dist: {trip.get('rideDistance')} km")
                        
                        col4.metric("Fare", f"${trip.get('bookingValue')}")
                        col4.caption(f"Payment: {trip.get('paymentMethod')}")
                        
                        if st.button("🗑️ Delete This Trip", key=f"del_{lookup_id}"):
                            pass
                            
                elif res.status_code == 404:
                    st.warning(f"🚫 Trip '{lookup_id}' not found.")
                else:
                    st.error(f"Error: {res.status_code} - {res.text}")
                    
            except Exception as e:
                st.error(f"Connection Error: {e}")

    st.subheader("📋 Trip Explorer")

    if "trips_data" not in st.session_state:
        st.session_state.trips_data = pd.DataFrame()
    if "current_page" not in st.session_state:
        st.session_state.current_page = 1
    if "data_fully_loaded" not in st.session_state:
        st.session_state.data_fully_loaded = False

    def reset_search():
        st.session_state.search_cid = ""
        st.session_state.trips_data = pd.DataFrame()
        st.session_state.current_page = 1
        st.session_state.data_fully_loaded = False

    def load_trips():
        if st.session_state.data_fully_loaded:
            return

        try:
            req_params = {'page': st.session_state.current_page, 'pageSize': 50}
            
            search_text = st.session_state.get("search_cid", "").strip()
            if search_text:
                req_params['customerId'] = search_text

            res = requests.get(f"{API_BASE_URL}/Trips", params=req_params)
            
            if res.status_code == 200:
                new_data = res.json()
                
                if st.session_state.current_page == 1:
                     st.session_state.trips_data = pd.DataFrame(new_data)
                else:
                    if len(new_data) > 0:
                        df_new = pd.DataFrame(new_data)
                        st.session_state.trips_data = pd.concat([st.session_state.trips_data, df_new], ignore_index=True)
                
                if len(new_data) < 50:
                    st.session_state.data_fully_loaded = True
                else:
                    st.session_state.current_page += 1
            else:
                st.error(f"API Error: {res.text}")
        except Exception as e:
            st.error(f"Connection Error: {e}")

    col_search, col_reset = st.columns([4, 1])
    with col_search:
        st.text_input(
            "Search by Customer ID:", 
            key="search_cid", 
            placeholder="Type ID (e.g. CID123) and press Enter...",
            on_change=lambda: [st.session_state.update(trips_data=pd.DataFrame(), current_page=1, data_fully_loaded=False), load_trips()]
        )
    with col_reset:
        st.write("")
        st.write("")
        st.button("🔄 Reset", on_click=reset_search)

    if st.session_state.trips_data.empty and not st.session_state.data_fully_loaded:
        load_trips()

    if not st.session_state.trips_data.empty:
        total_rows = len(st.session_state.trips_data)
        st.info(f"Showing {total_rows} trips...")

        st.dataframe(
            st.session_state.trips_data, 
            use_container_width=True,
            column_config={
                "bookingId": "ID",
                "bookingStatus": "Status",
                "bookingValue": st.column_config.NumberColumn("Cost", format="$%.2f"),
                "bookingDate": st.column_config.DateColumn("Date")
            }
        )
        
        if not st.session_state.data_fully_loaded:
            if st.button("⬇️ Load More (Next 50)", use_container_width=True):
                load_trips()
                st.rerun()
        else:
            if total_rows > 0:
                st.caption("✅ End of list.")
    else:
        st.write("No trips found.")