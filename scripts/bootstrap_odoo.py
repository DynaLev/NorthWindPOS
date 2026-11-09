admin = env.ref("base.user_admin")
company = env.company

if not company.country_id:
    country = env.ref("base.ec")
    company.write({"country_id": country.id})

if not company.chart_template:
    env["account.chart.template"].try_loading("generic_coa", company)

journal = env["account.journal"].search([("type", "=", "sale"), ("company_id", "=", company.id)], limit=1)

def ensure_product(name, price, code):
    product = env["product.product"].search([("default_code", "=", code)], limit=1)
    vals = {
        "name": name,
        "default_code": code,
        "list_price": price,
        "available_in_pos": True,
        "sale_ok": True,
        "type": "consu",
    }
    if product:
        product.write(vals)
    else:
        product = env["product.product"].create(vals)
    return product

cafe = ensure_product("Cafe Norte", 2.50, "NW-CAFE")
sandwich = ensure_product("Sandwich Norte", 4.00, "NW-SAND")

config = env["pos.config"].search([("name", "=", "Northwind Caja")], limit=1)
if not config:
    config = env["pos.config"].with_user(admin).create({"name": "Northwind Caja"})
if journal and not config.journal_id:
    config.journal_id = journal.id

config = config.with_user(admin)
if not config.current_session_id:
    config.open_ui()
session = config.current_session_id
if session.state == "opening_control":
    session.set_opening_control(0, "Apertura Northwind")

existing = env["res.users.apikeys"].sudo().search([("name", "=", "NorthWindPOS"), ("user_id", "=", admin.id)])
if not existing:
    api_key = env["res.users.apikeys"].with_user(admin).sudo()._generate("rpc", "NorthWindPOS", False)
    print("APIKEY", api_key)
else:
    print("APIKEY_EXISTS", existing.ids)

env.cr.commit()
cash = config.payment_method_ids.filtered(lambda method: method.type == "cash")[:1]
print("CONFIG", config.id)
print("SESSION", session.id, session.state)
print("CASH", cash.id if cash else 0, cash.name if cash else "")
print("PRODUCTS", cafe.id, sandwich.id)
